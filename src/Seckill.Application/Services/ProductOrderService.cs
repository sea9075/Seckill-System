using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class ProductOrderService : IProductOrderService
{
    private const int MaxRetries = 3;
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

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            product.DecreaseStock(quantity); // Domain 不變性檢查

            try
            {
                await _productRepository.UpdateAsync(product);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductId = productId,
                    ActivityId = null,
                    Quantity = quantity,
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    Status = OrderStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepository.AddAsync(order);
                return order;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == MaxRetries)
                    throw new InvalidOperationException("目前搶購人數過多，請稍後再試");

                // RowVersion 不一致：這段期間有別的請求先改了這筆商品
                // 把 product 還原成資料庫最新值（含新的 Stock、新的 RowVersion）再試一次
                // 不能重新呼叫 GetByIdAsync
                await _productRepository.ReloadAsync(product);
            }
        }

        throw new InvalidOperationException("目前搶購人數過多，請稍後再試");
    }
}