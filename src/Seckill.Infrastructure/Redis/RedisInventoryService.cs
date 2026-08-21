using Seckill.Application.Interfaces;
using StackExchange.Redis;

namespace Seckill.Infrastructure.Redis;

public class RedisInventoryService : IRedisInventoryService
{
    public const string OrderStreamKey = "seckill:orders:stream";

    private const string DecrementAndEnqueueScript = @"
        local stockKey = KEYS[1]
        local streamKey = KEYS[2]
        local quantity = tonumber(ARGV[1])
        local orderId = ARGV[2]
        local userId = ARGV[3]
        local productId = ARGV[4]
        local activityId = ARGV[5]

        local currentStock = tonumber(redis.call('GET', stockKey))
        if currentStock == nil then
            return -1
        end
        if currentStock < quantity then
            return -2
        end

        redis.call('DECRBY', stockKey, quantity)
        redis.call('XADD', streamKey, '*',
            'orderId', orderId, 'userId', userId, 'productId', productId,
            'activityId', activityId, 'quantity', quantity)

        return currentStock - quantity
    ";

    private readonly IConnectionMultiplexer _redis;
    private static string StockKey(int activityId) => $"seckill:stock:{activityId}";

    public RedisInventoryService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<(StockDecrementtResult, long)> TryDecrementAndEnqueueAsync(
        int activityId, int quantity, Guid orderId, Guid userId, int productId
    )
    {
        var db = _redis.GetDatabase();

        var raw = await db.ScriptEvaluateAsync(
            DecrementAndEnqueueScript,
            new RedisKey[] {StockKey(activityId), OrderStreamKey},
            new RedisValue[] {quantity, orderId.ToString(), userId.ToString(), productId, activityId}
        );

        var result = (long)raw;

        return result switch
        {
            -1 => (StockDecrementtResult.NotFound, 0),
            -2 => (StockDecrementtResult.OutOfStock, 0),
            _ => (StockDecrementtResult.Success, result)
        };
    }

    public async Task SetStockAsync(int activityId, int stock)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(StockKey(activityId), stock);
    }
}