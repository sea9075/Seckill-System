using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product> CreateAsync(string name, decimal price, int stock); // Admin only
    Task UpdateAsync(int id, string name, decimal price, int stock); // Admin only
}