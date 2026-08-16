using Microsoft.EntityFrameworkCore;

namespace Seckill.Infrastructure.Persistence;

public class SeckillDbContext : DbContext
{
    public SeckillDbContext(DbContextOptions<SeckillDbContext> options): base(options)
    {
        
    }
}
