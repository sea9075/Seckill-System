using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task ReloadAsync(Product product); // 用資料庫最新值覆蓋這個已追蹤的物件，樂觀鎖重試專用
}