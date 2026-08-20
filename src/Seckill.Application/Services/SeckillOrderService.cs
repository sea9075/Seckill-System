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
        var activity = await _activityRepository.GetByIdAsync(activityId) ??
            throw new InvalidOperationException("活動不存在");

        if (!activity.IsOngoing(DateTime.UtcNow))
            throw new InvalidOperationException("活動未開始或已結束");

        var (result, _) = await _redisInventoryService.TryDecrementStockAsync(activityId, 1);

        switch (result)
        {
            case StockDecrementtResult.NotFound:
                // Redis 裡的庫存 key 不見了（例如 Redis 重啟遺失資料）
                // 嘗試從 MSSQL 重建，讓使用者重新搶購一次，而不是直接判定這次搶購失敗
                // 就算同一瞬間有上千個請求都走到這裡，SyncStockToRedisAsync 內部的分散式鎖
                // 也只會讓其中一個實例真正執行重建，其他人會直接跳過
                await _stockSyncService.SyncStockToRedisAsync(activityId, activity.SeckillStock);
                throw new InvalidOperationException("系統剛完成庫存校正，請重新搶購一次");
            case StockDecrementtResult.OutOfStock:
                throw new InvalidOperationException("手慢了，庫存已被搶完");
        }

        // 注意：這篇（Step 3）先維持同步寫 DB，Order 建立完成才回應使用者
        // Phase 4 Step 5 會把這裡改成推進 Redis Stream，交給獨立的 Worker 非同步寫入
        // 屆時這支方法回應給前端的語意也會跟著改變
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

        await _orderRepository.AddAsync(order);
        return order;
    }
}