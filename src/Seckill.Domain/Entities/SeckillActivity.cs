namespace Seckill.Domain.Entities;

public class SeckillActivity
{
    public int Id { get; set; }
    public int ProductId { get; private set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int SeckillStock { get; private set; }
    public bool IsHighTraffic { get; private set; }

    private SeckillActivity() { } // EF Core 需要無參數建構子

    // EndTime > StartTime、SeckillStock 不能是負數，只看參數本身就能判斷，屬於不變性
    // ProductId 存不存在需要查 Repository，這種要問外部資料的規則留在 Application 層
    public static SeckillActivity Create (int productId, DateTime start, DateTime end, int stock, bool IsHighTraffic)
    {
        if (end <= start)
            throw new ArgumentException("結束時間必須晚於開始時間", nameof(end));
        if (stock < 0)
            throw new ArgumentException("秒殺庫存不能是負數", nameof(stock));

        return new SeckillActivity
        {
            ProductId = productId,
            StartTime = start,
            EndTime = end,
            SeckillStock = stock,
            IsHighTraffic = IsHighTraffic
        };
    }

    public bool IsOngoing(DateTime now) => now >= StartTime && now <= EndTime;

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("扣減數量必須大於 0", nameof(quantity));
        if (SeckillStock - quantity < 0)
            throw new InvalidOperationException("秒殺庫存不足");
        
        SeckillStock -= quantity;
    }
}