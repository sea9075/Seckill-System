using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seckill.Application.Interfaces;

namespace Seckill.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
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
}

public record CreateProductRequest(string Name, decimal Price, int Stock);