using Microsoft.Extensions.Logging;
using Seckill.Application.Interfaces;

namespace Seckill.Application.Services;

public class SeckillStockSyncService : ISeckillStockSyncService
{
    private readonly IDistributedLockService _lockService;
    private readonly IRedisInventoryService _redisInventoryService;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<SeckillStockSyncService> _logger;

    public SeckillStockSyncService(
        IDistributedLockService lockService,
        IRedisInventoryService redisInventoryService,
        IOrderRepository orderRepository,
        ILogger<SeckillStockSyncService> logger
    )
    {
        _lockService = lockService;
        _redisInventoryService = redisInventoryService;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task SyncStockToRedisAsync(int activityId, int originalStock)
    {
        await using var lockHandle = await _lockService.TryAcquireAsync(
            $"seckill-stock-sync:{activityId}", TimeSpan.FromSeconds(10)
        );

        if (lockHandle is null)
        {
            // 沒搶到鎖，代表別的實例正在做同一件事，讓它做完就好，不用重複執行
            _logger.LogInformation("[{Instance}] 沒搶到鎖，略過同步", Environment.MachineName + ":" + Environment.ProcessId);
            return;
        }
        _logger.LogInformation("[{Instance}] 搶到鎖，執行庫存重建", Environment.MachineName + ":" + Environment.ProcessId);

        // MSSQL 的 Orders 表才是「這個活動賣出多少件」的權威來源
        // 從 Step 3 開始，MSSQL 的 SeckillActivities.SeckillStock 欄位不會再被扣減
        // 一直保持「原始庫存」的值，所以正確的剩餘庫存 = 原始庫存 - 已成立的訂單數
        var soldCount = await _orderRepository.CountConfirmedByActivityIdAsync(activityId);
        var remaining = Math.Max(0, originalStock - soldCount);

        await _redisInventoryService.SetStockAsync(activityId, remaining);
    }
}