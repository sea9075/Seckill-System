namespace Seckill.Application.Interfaces;

public interface ISeckillStockSyncService
{
    Task SyncStockToRedisAsync(int activityId, int originalStock);
}