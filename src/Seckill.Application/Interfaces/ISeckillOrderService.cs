using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface ISeckillOrderService
{
    Task<Order> PlaceOrderAsync(Guid userId, int activityId);
}