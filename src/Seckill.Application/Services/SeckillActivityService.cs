using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class SeckillActivityService : ISeckillActivityService
{
    private readonly ISeckillActivityRepository _activityRepository;
    private readonly IProductRepository _productRepository;

    public SeckillActivityService(ISeckillActivityRepository activityRepository, IProductRepository productRepository)
    {
        _activityRepository = activityRepository;
        _productRepository = productRepository;
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
        return activity;
    }
}