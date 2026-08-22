using System.Text.Json;
using Seckill.Application.Interfaces;
using StackExchange.Redis;

namespace Seckill.Infrastructure.Caching;

public class ProductCacheService : IProductCacheService
{
    private const string NullMarker = "__NULL__"; // 快取穿透防護：連「查無此商品」都快取起來
    private static readonly Random Jitter = new();

    private readonly IProductRepository _productRepository; // 真正查 MSSQL 的那個
    private readonly IConnectionMultiplexer _redis;
    private readonly IDistributedLockService _lockService; // 分散式鎖，做快取擊穿防護

    public ProductCacheService(
        IProductRepository productRepository,
        IConnectionMultiplexer redis,
        IDistributedLockService lockService
    )
    {
        _productRepository = productRepository;
        _redis = redis;
        _lockService = lockService;
    }

    private async Task<ProductDto?> LoadAndCacheAsync(IDatabase db, string cacheKey, int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            await db.StringSetAsync(cacheKey, NullMarker, TimeSpan.FromSeconds(30) + TimeSpan.FromSeconds(Jitter.Next(0, 10)));
            return null;
        }

        var dto = new ProductDto(product.Id, product.Name, product.Price, product.Stock);

        // 快取雪崩防護：TTL 加上隨機抖動，避免大量商品的快取在同一秒集體過期
        var ttl = TimeSpan.FromSeconds(5) + TimeSpan.FromSeconds(Jitter.Next(0, 60));
        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(dto), ttl);

        return dto;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var db = _redis.GetDatabase();
        var cacheKey = $"product:{id}";

        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return cached == NullMarker ? null : JsonSerializer.Deserialize<ProductDto>((string)cached!);
        }

        // 快取擊穿防護：快取沒命中時，先搶鎖再重建，同一時間只讓一個請求真的去查 DB
        // 其他同時進來的請求搶不到鎖，稍等一下再重讀一次快取（大機率這時候已經被重建好了）
        await using var lockHandle = await _lockService.TryAcquireAsync($"product-rebuild:{id}", TimeSpan.FromSeconds(5));

        if (lockHandle is null)
        {
            await Task.Delay(100);
            var retryCache = await db.StringGetAsync(cacheKey);

            if (retryCache.HasValue)
            {
                return retryCache == NullMarker ? null : JsonSerializer.Deserialize<ProductDto>((string)retryCache!);
            }
            
            // 極端情況下還是沒等到重建完成，直接查一次 DB 回應使用者
            // 寧可多查一次 DB，也不要讓使用者平白拿到失敗結果
            var fallback = await _productRepository.GetByIdAsync(id);
            
            return fallback is null ? null : new ProductDto(fallback.Id, fallback.Name, fallback.Price, fallback.Stock);
        }

        return await LoadAndCacheAsync(db, cacheKey, id);
    }

    public async Task InvalidateAsync(int id)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"product:{id}");
    }
}