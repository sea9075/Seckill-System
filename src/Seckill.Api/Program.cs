using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Seckill.Application.Interfaces;
using Seckill.Application.Services;
using Seckill.Infrastructure.Persistence;
using Seckill.Infrastructure.Redis;
using Seckill.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ========== 以下 services（註冊進 DI Container）==========
builder.Services.AddDbContext<SeckillDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);
// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!)
);

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductOrderService, ProductOrderService>();
builder.Services.AddScoped<IRedisInventoryService, RedisInventoryService>();
builder.Services.AddScoped<ISeckillOrderService, SeckillOrderService>();
builder.Services.AddScoped<ISeckillActivityService, SeckillActivityService>();
builder.Services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddScoped<ISeckillStockSyncService, SeckillStockSyncService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISeckillActivityRepository, SeckillActivityRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();

// ========== 以下 middleware / routes（照實際執行順序排列，兩者在這裡是交錯的）==========
// app.UseHttpsRedirection(); // 本機開發先不用，交給 Azure／reverse proxy 處理
app.UseCors("AllowReactDev");

// 全域例外處理：把 Domain / Application 丟出來的例外轉成乾淨的 HTTP 狀態碼
// 不然 ArgumentException 這類例外沒人接住，最後會變成對前端沒有意義的 500
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        (int statusCode, string message) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "系統發生未預期錯誤")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { message });
    });
});

app.UseAuthentication(); // 一定要在 UseAuthorization 之前，不然 [Authorize] 會全部回 401
app.UseAuthorization();

// ========== 以下 routes ==========
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // 互動式介面，路由在 /scalar
}
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));
app.MapControllers();

app.Run();