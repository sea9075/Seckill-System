using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface IProductOrderService
{
    Task<Order> PlaceOrderAsync(Guid userId, int productId, int quantity);
}