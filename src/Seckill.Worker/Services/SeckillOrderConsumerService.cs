using Microsoft.EntityFrameworkCore;
using Seckill.Application.Interfaces;
using Seckill.Domain.Entities;
using Order = Seckill.Domain.Entities.Order;
using Seckill.Infrastructure.Redis;
using StackExchange.Redis;

namespace Seckill.Worker.Services;

public class SeckillOrderConsumerService : BackgroundService
{
    private const string ConsumerGroup = "seckill-order-consumers";

    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeckillOrderConsumerService> _logger;
    private readonly string _consumerName = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";

    public SeckillOrderConsumerService(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<SeckillOrderConsumerService> logger
    )
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private async Task EnsureConsumerGroupExistsAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                RedisInventoryService.OrderStreamKey, ConsumerGroup, StreamPosition.NewMessages
            );
        } catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Consumer group 已經存在（例如 Worker 重啟過），不是錯誤，忽略即可
        }
    }

    private async Task ProcessEntryAsync(IDatabase db, StreamEntry entry, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var fields = entry.Values.ToDictionary(f => (string)f.Name!, f => (string)f.Value!);
        var orderId = Guid.Parse(fields["orderId"]);

        var order = new Order
        {
            Id = orderId,
            UserId = Guid.Parse(fields["userId"]),
            ProductId = int.Parse(fields["productId"]),
            ActivityId = int.Parse(fields["activityId"]),
            Quantity = int.Parse(fields["quantity"]),
            IdempotencyKey = orderId.ToString(),
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await orderRepository.AddAsync(order);
        }
        catch (DbUpdateException)
        {
            // Unique constraint 衝突：這筆訊息之前處理過了（at-least-once 重複投遞造成）
            // 這裡先簡化成「捕捉到寫入例外就當作重複、直接 Ack」
            _logger.LogWarning("訂單 {OrderId} 可能重複投遞，略過寫入", orderId);
        }

        await db.StreamAcknowledgeAsync(RedisInventoryService.OrderStreamKey, ConsumerGroup, entry.Id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        await EnsureConsumerGroupExistsAsync(db);

        while (!stoppingToken.IsCancellationRequested)
        {
            var entries = await db.StreamReadGroupAsync(
                RedisInventoryService.OrderStreamKey, ConsumerGroup, _consumerName, ">", count: 10
            );

            if (entries.Length == 0)
            {
                await Task.Delay(500, stoppingToken); // 沒有新訊息，避免空轉狂打 Redis
                continue;
            }

            foreach (var entry in entries)
            {
                await ProcessEntryAsync(db, entry, stoppingToken);
            }
        }
    }
}