using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;
using Seckill.Infrastructure.Persistence;

namespace Seckill.Infrastructure.Repositories;

public class UserRepository: IUserRepository
{
    private readonly SeckillDbContext _dbContext;

    public UserRepository(SeckillDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }
}