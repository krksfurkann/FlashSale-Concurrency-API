namespace FlashSale.Api.DTOs;

public class CreateOrderDto
{
    /// <summary>
    /// Sipariş verilecek flaş kampanyanın Kimliği (Id)
    /// </summary>
    public int CampaignId { get; set; }

    /// <summary>
    /// Siparişi veren kullanıcının Kimliği (Id)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Satın alınmak istenen ürün adedi (Varsayılan: 1)
    /// </summary>
    public int Quantity { get; set; } = 1;
}