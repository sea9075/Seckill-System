using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Infrastructure.Persistence;
using Seckill.Infrastructure.Repositories;
using Seckill.Worker.Services;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<SeckillDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddHostedService<SeckillOrderConsumerService>();

var host = builder.Build();
host.Run();