using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface ISeckillActivityService
{
    Task<List<SeckillActivity>> GetAllAsync();
    Task<SeckillActivity> CreateAsync(int productId, DateTime start, DateTime end, int stock, bool isHighTraffic); // Admin only
}