namespace Seckill.Application.Interfaces;

public record ProductDto(int Id, string Name, decimal Price, int Stock);

public interface IProductCacheService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task InvalidateAsync(int id);
}