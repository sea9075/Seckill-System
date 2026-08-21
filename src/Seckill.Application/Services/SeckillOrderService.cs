using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

public class SeckillOrderService : ISeckillOrderService
{
    private readonly ISeckillActivityRepository _activityRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRedisInventoryService _redisInventoryService;
    private readonly ISeckillStockSyncService _stockSyncService;

    public SeckillOrderService(
        ISeckillActivityRepository activityRepository,
        IOrderRepository orderRepository,
        IRedisInventoryService redisInventoryService,
        ISeckillStockSyncService stockSyncService
        )
    {
        _activityRepository = activityRepository;
        _orderRepository = orderRepository;
        _redisInventoryService = redisInventoryService;
        _stockSyncService = stockSyncService;
    }

    public async Task<Order> PlaceOrderAsync(Guid userId, int activityId)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId)
            ?? throw new InvalidOperationException("活動不存在");

        if (!activity.IsOngoing(DateTime.UtcNow))
            throw new InvalidOperationException("活動未開始或已結束");

        var orderId = Guid.NewGuid(); // Id 現在由 Producer 先產生，一路帶到 Consumer
        
        var (result, _) = await _redisInventoryService.TryDecrementAndEnqueueAsync(
            activityId, 1, orderId, userId, activity.ProductId
        );

        switch (result)
        {
            case StockDecrementtResult.NotFound:
                await _stockSyncService.SyncStockToRedisAsync(activityId, activity.SeckillStock);
                throw new InvalidOperationException("系統剛完成庫存校正，請重新搶購一次");
            case StockDecrementtResult.OutOfStock:
                throw new InvalidOperationException("手慢了，庫存已被搶完");
        }

        // 注意：這裡回傳的 Order 還沒真正寫進 MSSQL，Status 是 Pending
        // 代表「搶購成功、名額已保留」，實際的訂單記錄由 Seckill.Worker 非同步建立
        // 前端要改用 GET /api/orders/{id} 輪詢，直到 Status 變成 Confirmed 才算真正落地
        return new Order
        {
            Id = orderId,
            UserId = userId,
            ProductId = activity.ProductId,
            ActivityId = activity.Id,
            Quantity = 1,
            IdempotencyKey = string.Empty,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}