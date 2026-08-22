using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;
using Seckill.Infrastructure.Persistence;

namespace Seckill.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly SeckillDbContext _dbContext;
    private readonly ILogger _logger;

    public ProductRepository(SeckillDbContext dbContext, ILogger<ProductRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _dbContext.Products.ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        _logger.LogInformation("實際查詢 DB：ProductId={Id}", id);
        return await _dbContext.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        // product 是從 GetByIdAsync 查出來的，EF Core 已經在追蹤它（tracked）
        // 呼叫 SaveChangesAsync 就會自動把異動寫回去，不用再手動呼叫 _dbContext.Products.Update(product)
        await _dbContext.SaveChangesAsync();
    }

    public async Task ReloadAsync(Product product)
    {
        await _dbContext.Entry(product).ReloadAsync();
    }
}