using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;
using Seckill.Infrastructure.Persistence;

namespace Seckill.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly SeckillDbContext _dbContext;

    public OrderRepository(SeckillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Order order)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Orders.FindAsync(id);
    }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.Orders.Where(o => o.UserId == userId).ToListAsync();
    }

    public async Task<int> CountConfirmedByActivityIdAsync(int activityId)
    {
        return await _dbContext.Orders.CountAsync(
            o => o.ActivityId == activityId && o.Status == OrderStatus.Confirmed
        );
    }
}