using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seckill.Application.Interfaces;

namespace Seckill.Api.Controllers;

[ApiController]
[Route("api/seckill-activities")]
public class SeckillActivitiesController : ControllerBase
{
    private readonly ISeckillActivityService _seckillActivityService;

    public SeckillActivitiesController(ISeckillActivityService seckillActivityService)
    {
        _seckillActivityService = seckillActivityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _seckillActivityService.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSeckillActivityRequest request)
    {
        var activity = await _seckillActivityService.CreateAsync(
            request.ProductId, request.Start, request.End, request.Stock, request.IsHighTraffic
        );

        return Ok(activity);
    }
}

public record CreateSeckillActivityRequest(int ProductId, DateTime Start, DateTime End, int Stock, bool IsHighTraffic);