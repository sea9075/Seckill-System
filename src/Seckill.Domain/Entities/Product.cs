namespace Seckill.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public byte[] RowVersion { get; set; } = default!; // Phase 4 Step 2 才會真正拿來做樂觀鎖

    private Product() { } // EF Core 需要無參數建構子，query 資料時會用到

    // Create 跟 Update 的驗證規則完全一樣，抽成一個 private static helper 給兩邊共用，避免重複。
    private static void Validate(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("商品名稱不能是空的", nameof(name));
        if (price <= 0)
            throw new ArgumentException("價格必須大於 0", nameof(price));
        if (stock < 0)
            throw new ArgumentException("庫存不能是負數", nameof(stock));
    }

    // 靜態工廠方法：建立一筆全新的商品
    // 只看參數本身就能判斷合不合理的檢查（不變性）放在這裡，不是 Application 層
    // 這樣不管是 Controller 走過來的，還是之後 Phase 4 的 Consumer、或任何測試程式碼建立 Product
    // 都一定會經過同一套驗證，不會有人繞過去
    public static Product Create(string name, decimal price, int stock)
    {
        Validate(name, price, stock);
        return new Product { Name = name, Price = price, Stock = stock };
    }

    // 修改一個已經存在的商品——物件已經存在、要改自己的狀態，所以是 instance method，不是 static
    public void Update(string name, decimal price, int stock)
    {
        Validate(name, price, stock);
        Name = name;
        Price = price;
        Stock = stock;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("扣減數量必須大於 0", nameof(quantity));
        if (Stock - quantity < 0)
            throw new InvalidOperationException("庫存不足");

        Stock -= quantity;
    }
}