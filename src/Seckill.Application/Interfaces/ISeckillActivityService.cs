using Seckill.Domain.Entities;

public interface ISeckillActivityService
{
    Task<List<SeckillActivity>> GetAllAsync();
    Task<SeckillActivity> CreateAsync(int productId, DateTime start, DateTime end, int stock, bool isHighTraffic); // Admin only
}