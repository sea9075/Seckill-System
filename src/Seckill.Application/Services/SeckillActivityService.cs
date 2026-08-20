using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class SeckillActivityService : ISeckillActivityService
{
    private readonly ISeckillActivityRepository _activityRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISeckillStockSyncService _stockSyncService;

    public SeckillActivityService(
        ISeckillActivityRepository activityRepository,
        IProductRepository productRepository,
        ISeckillStockSyncService stockSyncService
        )
    {
        _activityRepository = activityRepository;
        _productRepository = productRepository;
        _stockSyncService = stockSyncService;
    }

    public async Task<List<SeckillActivity>> GetAllAsync()
    {
        return await _activityRepository.GetAllAsync();
    }

    public async Task<SeckillActivity> CreateAsync(int productId, DateTime start, DateTime end, int stock, bool isHighTraffic)
    {
        _ = await _productRepository.GetByIdAsync(productId) ??
            throw new InvalidOperationException("找不到對應的商品");

        var activity = SeckillActivity.Create(productId, start, end, stock, isHighTraffic);
        await _activityRepository.AddAsync(activity);

        if (isHighTraffic)
        {
            await _stockSyncService.SyncStockToRedisAsync(activity.Id, stock);
        }
        
        return activity;
    }
}