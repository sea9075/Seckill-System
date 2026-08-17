using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _productRepository.GetAllAsync();
    }

    public async Task<Product> CreateAsync(string name, decimal price, int stock)
    {
        var product = Product.Create(name, price, stock);

        await _productRepository.AddAsync(product);
        return product;
    }

    public async Task UpdateAsync(int id, string name, decimal price, int stock)
    {
        var existing = await _productRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("商品不存在");
        
        existing.Update(name, price, stock); // 驗證跟賦值都在 Product.Update() 裡完成

        await _productRepository.UpdateAsync(existing);
    }
}