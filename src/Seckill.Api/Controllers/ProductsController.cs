using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seckill.Application.Interfaces;

namespace Seckill.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductCacheService _productionCacheService;

    public ProductsController(IProductService productService, IProductCacheService productCacheService)
    {
        _productService = productService;
        _productionCacheService = productCacheService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _productService.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(
            request.Name, request.Price, request.Stock
        );

        return Ok(product);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productionCacheService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }
}

public record CreateProductRequest(string Name, decimal Price, int Stock);