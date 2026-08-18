using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IProductOrderService _productOrderService;
    private readonly ISeckillOrderService _seckillOrderService;
    private readonly IOrderRepository _orderRepository;

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            throw new InvalidOperationException("找不到使用者識別碼");

        return Guid.Parse(sub);
    }

    public OrdersController(IProductOrderService productOrderService, ISeckillOrderService seckillOrderService, IOrderRepository orderRepository)
    {
        _productOrderService = productOrderService;
        _seckillOrderService = seckillOrderService;
        _orderRepository = orderRepository;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var userId = GetCurrentUserId();

        Order order = request.ActivityId is not null ?
            await _seckillOrderService.PlaceOrderAsync(userId, request.ActivityId.Value) :
            await _productOrderService.PlaceOrderAsync(userId, request.ProductId!.Value, request.Quantity);

        return Ok(order);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order is null) return NotFound();
        if (order.UserId != GetCurrentUserId()) return Forbid(); // 存在，但不是你的訂單

        return Ok(order);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetCurrentUserId();
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        
        return Ok(orders);
    }
}

public record PlaceOrderRequest(int? ProductId, int? ActivityId, int Quantity = 1);