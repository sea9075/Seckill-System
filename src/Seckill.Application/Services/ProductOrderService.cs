using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class ProductOrderService : IProductOrderService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductOrderService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Order> PlaceOrderAsync(Guid userId, int productId, int quantity)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new InvalidOperationException("商品不存在");

        // 刻意的競態條件：讀取跟扣減之間沒有任何鎖
        // 兩個併發請求都可能讀到 Stock > 0，都通過檢查，都執行扣減 → 超賣
        product.DecreaseStock(quantity);
        await _productRepository.UpdateAsync(product);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            ActivityId = null,
            Quantity = quantity,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Status = OrderStatus.Confirmed, // MVP 模擬付款：下單成功 = 視為付款完成，不另外做付款 API
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddAsync(order);
        return order;
    }
}