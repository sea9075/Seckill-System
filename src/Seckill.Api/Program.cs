using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Seckill.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // 互動式介面，路由在 /scalar
    app.MapScalarApiReference();
}

// app.UseHttpsRedirection();
app.UseCors("AllowReactDev");
app.MapGet("/api/health", () => Results.Ok(new {status = "ok", timestamp = DateTime.UtcNow}));

app.UseAuthorization();

app.MapControllers();

app.Run();