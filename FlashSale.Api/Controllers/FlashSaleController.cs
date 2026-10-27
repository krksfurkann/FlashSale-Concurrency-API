using FlashSale.Api.DTOs;
using FlashSale.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlashSaleController : ControllerBase
{
    private readonly OrderService _orderService;

    public FlashSaleController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Aktif kampanyaları ve stok durumunu getirir.
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetActiveCampaigns()
    {
        var campaigns = await _orderService.GetActiveCampaignsAsync();
        return Ok(campaigns);
    }

    /// <summary>
    /// Flaş indirimli ürün siparişi oluşturur (Güvenli ve atomik stok düşme).
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (dto.Quantity <= 0)
        {
            return BadRequest(new { Success = false, Message = "Sipariş adedi en az 1 olmalıdır." });
        }

        var result = await _orderService.CreateFlashOrderAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { Success = false, Message = result.Message });
        }

        return Ok(new
        {
            Success = true,
            Message = result.Message,
            OrderId = result.OrderId
        });
    }
}