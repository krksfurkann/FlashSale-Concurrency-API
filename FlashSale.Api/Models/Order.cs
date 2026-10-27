namespace FlashSale.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public int CampaignId { get; set; }
    public int UserId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }

}