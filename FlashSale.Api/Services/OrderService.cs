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
        // >>> DEĞİŞİKLİK 1: Adet 1'den küçükse (0, -5 gibi) hiçbir şey yapmadan reddet.
        if (dto.Quantity < 1)
            return (false, "Adet en az 1 olmalı.", null);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            // A. Önce kampanya var mı ve fiyatı nedir çekelim
            // >>> DEĞİŞİKLİK 2 (A kısmı): Kampanya süresi içinde mi diye iki satır eklendi.
            string campaignSql = @"
                SELECT DiscountedPrice FROM FlashCampaigns WITH (NOLOCK)
                WHERE Id = @CampaignId AND IsActive = 1
                  AND StartTime <= GETUTCDATE()
                  AND EndTime >= GETUTCDATE()";

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
            // >>> DEĞİŞİKLİK 2 (B kısmı): Aynı iki satır buraya da eklendi.
            string updateStockSql = @"
                UPDATE FlashCampaigns 
                SET RemainingStock = RemainingStock - @Quantity 
                WHERE Id = @CampaignId 
                  AND RemainingStock >= @Quantity 
                  AND IsActive = 1
                  AND StartTime <= GETUTCDATE()
                  AND EndTime >= GETUTCDATE()";

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
        // >>> DEĞİŞİKLİK 3: Hata ayrıntısı (ex.Message) artık kullanıcıya gösterilmiyor.
        catch (Exception)
        {
            transaction.Rollback();
            return (false, "Beklenmeyen bir hata oluştu.", null);
        }
    }
}