using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;

namespace Seckill.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductCacheService _productCacheService;

    public ProductService(IProductRepository productRepository, IProductCacheService productCacheService)
    {
        _productRepository = productRepository;
        _productCacheService = productCacheService;

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
        // 資料改了，舊快取要失效，不然使用者會看到舊資料
        await _productCacheService.InvalidateAsync(id);
    }
}