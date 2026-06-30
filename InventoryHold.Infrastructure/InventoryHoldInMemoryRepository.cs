using InventoryHold.Domain;
using InventoryHold.Domain.Repositories;

namespace InventoryHold.Infrastructure;

public class InventoryHoldInMemoryRepository : IInventoryHoldRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, InventoryItem> _inventory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HoldRecord> _holds = new(StringComparer.OrdinalIgnoreCase);

    public InventoryHoldInMemoryRepository()
    {
        SeedDefaultInventory();
    }

    public Task<InventoryItem?> GetInventoryItemAsync(string sku, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_inventory.TryGetValue(sku, out var item) ? Clone(item) : null);
        }
    }

    public Task<bool> ReserveInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_inventory.TryGetValue(sku, out var item) || item.AvailableQuantity < quantity)
            {
                return Task.FromResult(false);
            }

            item.AvailableQuantity -= quantity;
            return Task.FromResult(true);
        }
    }

    public Task<bool> RestoreInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_inventory.TryGetValue(sku, out var item))
            {
                return Task.FromResult(false);
            }

            item.AvailableQuantity += quantity;
            return Task.FromResult(true);
        }
    }

    public Task<HoldRecord> CreateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _holds[hold.HoldId] = Clone(hold);
            return Task.FromResult(Clone(hold));
        }
    }

    public Task<HoldRecord?> GetHoldAsync(string holdId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_holds.TryGetValue(holdId, out var hold) ? Clone(hold) : null);
        }
    }

    public Task<HoldRecord> UpdateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _holds[hold.HoldId] = Clone(hold);
            return Task.FromResult(Clone(hold));
        }
    }

    public Task<IReadOnlyList<InventoryItem>> ListInventoryAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<InventoryItem>>(_inventory.Values.Select(Clone).ToList());
        }
    }

    public Task<IReadOnlyList<HoldRecord>> ListActiveHoldsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<HoldRecord>>(_holds.Values.Where(x => x.Status == HoldStatus.Active).Select(Clone).ToList());
        }
    }

    public void SeedDefaultInventory()
    {
        lock (_sync)
        {
            _inventory["sku-1"] = new InventoryItem { Sku = "sku-1", Name = "Widget", AvailableQuantity = 10 };
            _inventory["sku-2"] = new InventoryItem { Sku = "sku-2", Name = "Gadget", AvailableQuantity = 8 };
            _inventory["sku-3"] = new InventoryItem { Sku = "sku-3", Name = "Thingamajig", AvailableQuantity = 5 };
            _inventory["sku-4"] = new InventoryItem { Sku = "sku-4", Name = "Doohickey", AvailableQuantity = 12 };
            _inventory["sku-5"] = new InventoryItem { Sku = "sku-5", Name = "Whatchamacallit", AvailableQuantity = 6 };
        }
    }

    private static InventoryItem Clone(InventoryItem item) => new() { Sku = item.Sku, Name = item.Name, AvailableQuantity = item.AvailableQuantity };

    private static HoldRecord Clone(HoldRecord hold) => new()
    {
        HoldId = hold.HoldId,
        CustomerId = hold.CustomerId,
        Status = hold.Status,
        Items = hold.Items.Select(x => new HoldItem { Sku = x.Sku, Quantity = x.Quantity }).ToList(),
        CreatedAt = hold.CreatedAt,
        ExpiresAt = hold.ExpiresAt
    };
}
