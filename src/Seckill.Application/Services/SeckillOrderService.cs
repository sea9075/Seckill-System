using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

public class SeckillOrderService : ISeckillOrderService
{
    private readonly ISeckillActivityRepository _activityRepository;
    private readonly IOrderRepository _orderRepository;

    public SeckillOrderService(ISeckillActivityRepository activityRepository, IOrderRepository orderRepository)
    {
        _activityRepository = activityRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Order> PlaceOrderAsync(Guid userId, int activityId)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId) ??
            throw new InvalidOperationException("活動不存在");
        
        if (!activity.IsOngoing(DateTime.UtcNow))
            throw new InvalidOperationException("活動未開始或已結束");

        // 跟 ProductOrderService 一樣的競態條件，這裡更明顯，因為秒殺場景衝突機率高很多
        activity.DecreaseStock(1);  // MVP 先假設秒殺一人限購 1 件，沒有這個假設的話兩個 Service 差異會更複雜

        // 注意：這裡沒有呼叫任何 Repository 方法去存 activity 的異動，靠的是 EF Core 的 change tracking
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = activity.ProductId,
            ActivityId = activity.Id,
            Quantity = 1,
            IdempotencyKey = Guid.NewGuid().ToString(),
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddAsync(order); // 這裡的 SaveChangesAsync 會一起存到 activity 的異動
        return order;
    }
}