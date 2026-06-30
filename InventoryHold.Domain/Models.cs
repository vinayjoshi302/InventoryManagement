namespace InventoryHold.Domain;

public class InventoryItem
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
}

public class HoldRecord
{
    public string HoldId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; }
    public List<HoldItem> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public class HoldItem
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public enum HoldStatus
{
    Active,
    Released,
    Expired
}
