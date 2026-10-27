using Dapper;
using FlashSale.Api.DTOs;
using FlashSale.Api.Models;
using Microsoft.Data.SqlClient;

namespace FlashSale.Api.Services;

public class OrderService
{
    private readonly string _connectionString;

    public OrderService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // 1. Aktif Kampanyaları Getir
    public async Task<IEnumerable<FlashCampaign>> GetActiveCampaignsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        string sql = @"
            SELECT * FROM FlashCampaigns 
            WHERE IsActive = 1 
              AND StartTime <= GETUTCDATE() 
              AND EndTime >= GETUTCDATE()";

        return await connection.QueryAsync<FlashCampaign>(sql);
    }

    // 2. Güvenli Stok Düşme & Sipariş Oluşturma (Atomic Operation)
    public async Task<(bool Success, string Message, Guid? OrderId)> CreateFlashOrderAsync(CreateOrderDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            // A. Önce kampanya var mı ve fiyatı nedir çekelim
            string campaignSql = @"
                SELECT DiscountedPrice FROM FlashCampaigns WITH (NOLOCK)
                WHERE Id = @CampaignId AND IsActive = 1";

            decimal? price = await connection.QueryFirstOrDefaultAsync<decimal?>(
                campaignSql,
                new { dto.CampaignId },
                transaction);

            if (price == null)
            {
                return (false, "Kampanya bulunamadı veya pasif durumda.", null);
            }

            // B. CRITICAL: Stok güncellemesini ATOMIC (Tek Hamlede) yapıyoruz!
            // 'WHERE RemainingStock >= @Quantity' şartı sayesinde aynı anda 100 kişi de gelse stok eksiye düşmez!
            string updateStockSql = @"
                UPDATE FlashCampaigns 
                SET RemainingStock = RemainingStock - @Quantity 
                WHERE Id = @CampaignId 
                  AND RemainingStock >= @Quantity 
                  AND IsActive = 1";

            int affectedRows = await connection.ExecuteAsync(
                updateStockSql,
                new { dto.Quantity, dto.CampaignId },
                transaction);

            if (affectedRows == 0)
            {
                // Yeterli stok yoksa transaction iptal edilir
                transaction.Rollback();
                return (false, "Üzgünüz, stok yetersiz veya tükenmiş!", null);
            }

            // C. Stok düşme başarılı ise Sipariş Kaydını oluşturalım
            Guid newOrderId = Guid.NewGuid();
            decimal totalPrice = price.Value * dto.Quantity;

            string insertOrderSql = @"
                INSERT INTO Orders (Id, CampaignId, UserId, Quantity, TotalPrice, CreatedAt)
                VALUES (@Id, @CampaignId, @UserId, @Quantity, @TotalPrice, GETUTCDATE())";

            await connection.ExecuteAsync(insertOrderSql, new
            {
                Id = newOrderId,
                dto.CampaignId,
                dto.UserId,
                dto.Quantity,
                TotalPrice = totalPrice
            }, transaction);

            // Her şey yolundaysa işlemi onaylayalım
            transaction.Commit();
            return (true, "Siparişiniz başarıyla oluşturuldu!", newOrderId);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return (false, $"Bir hata oluştu: {ex.Message}", null);
        }
    }
}