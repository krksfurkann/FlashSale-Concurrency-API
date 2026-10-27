namespace FlashSale.Api.Models;

public class FlashCampaign
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal DiscountedPrice { get; set; }
    public int TotalStock { get; set; }
    public int RemainingStock { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}