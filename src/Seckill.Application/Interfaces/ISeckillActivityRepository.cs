using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface ISeckillActivityRepository
{
    Task<List<SeckillActivity>> GetAllAsync();
    Task<SeckillActivity?> GetByIdAsync(int id);
    Task AddAsync(SeckillActivity activity);
}