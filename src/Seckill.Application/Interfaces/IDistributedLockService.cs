namespace Seckill.Application.Interfaces;

public interface IDistributedLockService
{
    /// <summary>
    /// 嘗試取得鎖。成功回傳一個 IAsyncDisposable，離開 using 範圍時自動釋放鎖；
    /// 鎖已被別人持有時回傳 null（呼叫端要自己決定失敗時要不要重試、等待或直接放棄）。
    /// </summary
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan expiry);
}