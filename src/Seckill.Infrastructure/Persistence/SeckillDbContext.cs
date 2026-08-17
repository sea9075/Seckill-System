using System.Data;
using Microsoft.EntityFrameworkCore;
using Seckill.Domain.Entities;

namespace Seckill.Infrastructure.Persistence;

public class SeckillDbContext : DbContext
{
    public SeckillDbContext(DbContextOptions<SeckillDbContext> options): base(options)
    {
        
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SeckillActivity> SeckillActivities => Set<SeckillActivity>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.RowVersion).HasColumnType("rowversion").ValueGeneratedOnAddOrUpdate();

            // 注意：這裡刻意「不」呼叫 .IsRowVersion() / .IsConcurrencyToken()
            // 差別在於：欄位型別還是 SQL Server 的 rowversion，資料庫每次 UPDATE 還是會自動更新這個值
            // 但 EF 產生的 UPDATE 語句不會把它加進 WHERE 條件，也就偵測不到併發衝突
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.IdempotencyKey).IsUnique();
        });
    }
}
