using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Seckill.Application.Interfaces;
using Seckill.Application.Services;
using Seckill.Infrastructure.Persistence;
using Seckill.Infrastructure.Repositories;

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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductOrderService, ProductOrderService>();
builder.Services.AddScoped<ISeckillOrderService, SeckillOrderService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISeckillActivityRepository, SeckillActivityRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// ========== 以下 middleware / routes（照實際執行順序排列，兩者在這裡是交錯的）==========
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // [routes] 開發環境專用：OpenAPI JSON 文件
    app.MapOpenApi();
    // 互動式介面，路由在 /scalar
    app.MapScalarApiReference();
}

// [middleware]（目前停用）本機開發不用 HTTPS 轉址，交給 Azure／reverse proxy 處理
// app.UseHttpsRedirection();

// [middleware] CORS：允許 React dev server 跨來源呼叫 API
app.UseCors("AllowReactDev");

// [routes] Health check：公開端點，不需要驗證
app.MapGet("/api/health", () => Results.Ok(new {status = "ok", timestamp = DateTime.UtcNow}));

// [middleware] Authorization
app.UseAuthorization();

// [routes] 其餘 API 路由，來自各個 Controller
app.MapControllers();

app.Run();