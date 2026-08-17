using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;
using Seckill.Infrastructure.Persistence;

namespace Seckill.Infrastructure.Repositories;

public class SeckillActivityRepository : ISeckillActivityRepository
{
    private readonly SeckillDbContext _dbContext;

    public SeckillActivityRepository(SeckillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SeckillActivity>> GetAllAsync()
    {
        return await _dbContext.SeckillActivities.ToListAsync();
    }

    public async Task<SeckillActivity?> GetByIdAsync(int id)
    {
        return await _dbContext.SeckillActivities.FindAsync(id);
    }

    public async Task AddAsync(SeckillActivity activity)
    {
        _dbContext.SeckillActivities.Add(activity);
        await _dbContext.SaveChangesAsync();
    }
}