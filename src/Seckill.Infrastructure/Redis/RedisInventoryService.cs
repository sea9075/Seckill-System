using Seckill.Application.Interfaces;
using StackExchange.Redis;

namespace Seckill.Infrastructure.Redis;

public class RedisInventoryService : IRedisInventoryService
{
    private const string DecrementScript = @"
        local stockKey = KEYS[1]
        local quantity = tonumber(ARGV[1])
        local currentStock = tonumber(redis.call('GET', stockKey))

        if currentStock == nil then
            return -1
        end
        if currentStock < quantity then
            return -2
        end

        redis.call('DECRBY', stockKey, quantity)
        return currentStock - quantity
    ";

    private static string StockKey(int activityId) => $"seckill:stock:{activityId}";
    private readonly IConnectionMultiplexer _redis;

    public RedisInventoryService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<(StockDecrementtResult, long)> TryDecrementStockAsync(int activityId, int quantity)
    {
        var db = _redis.GetDatabase();
        var raw = await db.ScriptEvaluateAsync(
            DecrementScript,
            new RedisKey[] { StockKey(activityId) },
            new RedisValue[] { quantity }
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