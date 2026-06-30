namespace InventoryHold.Contracts;

public class CreateHoldRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public List<HoldItemRequest> Items { get; set; } = [];
}

public class HoldItemRequest
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateHoldResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public List<HoldItemResponse> Items { get; set; } = [];
}

public class HoldItemResponse
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class InventoryItemResponse
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
}

public class HoldResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public List<HoldItemResponse> Items { get; set; } = [];
}

public class InventoryListResponse
{
    public List<InventoryItemResponse> Items { get; set; } = [];
}
