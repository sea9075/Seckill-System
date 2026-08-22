using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Seckill.Application.Interfaces;
using Seckill.Application.Services;
using Seckill.Infrastructure.Caching;
using Seckill.Infrastructure.Persistence;
using Seckill.Infrastructure.Redis;
using Seckill.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 全域限流：以「使用者」為 partition key，不同使用者的額度互不影響
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true ?
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous" :
            context.Connection.RemoteIpAddress?.ToString() ?? "unknow";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(10),
            SegmentsPerWindow = 5,
            QueueLimit = 0 // 秒殺場景寧可直接拒絕，不要排隊等待增加延遲
        });
    });

    // 秒殺下單這個熱點端點，額外套用更嚴格的 Token Bucket policy
    options.AddPolicy("SeckillOrderPolicy", context =>
    {
        var partitionKey = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,      // 桶子最多裝 5 個令牌，允許短暫連續搶購最多 5 次
            TokensPerPeriod = 1, // 之後每個補充週期回填 1 個令牌
            ReplenishmentPeriod = TimeSpan.FromSeconds(2),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new {message = "請求太頻繁，請稍後再試"}, cancellationToken: token
        );
    };
});

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
builder.Services.AddScoped<IProductCacheService, ProductCacheService>();

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
app.UseRateLimiter();

// ========== 以下 routes ==========
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // 互動式介面，路由在 /scalar
}
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));
app.MapControllers();

app.Run();