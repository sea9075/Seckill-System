using Seckill.Application.Interfaces;
using StackExchange.Redis;

namespace Seckill.Infrastructure.Redis;

public class RedisDistributedLockService : IDistributedLockService
{
    // 釋放鎖前要先確認 value 是自己當初設的 token 才刪除，不然可能發生：
    // A 拿到鎖 → A 因為某些原因執行太久，鎖過期自動失效 → B 搶到同一把鎖開始工作 → A 這時候才執行到釋放鎖的程式碼
    // 如果 A 沒檢查就直接 DEL，會把 B 正在用的鎖誤刪掉。「檢查 value 是不是自己的 + 刪除」必須是原子操作，一樣要用 Lua script
    private const string ReleaseScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end
    ";

    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{resource}";
        var token = Guid.NewGuid().ToString(); // 這台實例、這一次搶鎖的專屬憑證

        var acquired = await db.StringSetAsync(lockKey, token, expiry, When.NotExists);

        return acquired ? new RedisLockHandle(db, lockKey, token, ReleaseScript) : null;
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _lockKey;
        private readonly string _token;
        private readonly string _releaseScript;

        public RedisLockHandle(IDatabase db, string lockKey, string token, string releaseScript)
        {
            _db = db;
            _lockKey = lockKey;
            _token = token;
            _releaseScript = releaseScript;
        }

        public async ValueTask DisposeAsync()
        {
            await _db.ScriptEvaluateAsync(_releaseScript, new RedisKey[] {_lockKey}, new RedisValue[] {_token});
        }
    }
}