namespace Seckill.Application.Interfaces;

public enum StockDecrementtResult
{
    Success,
    NotFound,  // Redis 裡沒有這個活動的庫存 key
    OutOfStock // 庫存不足
}

public interface IRedisInventoryService
{
    Task<(StockDecrementtResult Result, long RemainingStock)> TryDecrementStockAsync(int activityId, int quantity);
    Task SetStockAsync(int activityId, int stock);
}