namespace Seckill.Domain.Entities;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Failed,
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int ProductId { get; set; }
    public int? ActivityId { get; set; } // 一般訂單沒有活動
    public int Quantity { get; set; }
    public string IdempotencyKey { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}