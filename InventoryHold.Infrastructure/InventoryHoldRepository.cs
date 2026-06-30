using InventoryHold.Domain;
using InventoryHold.Domain.Repositories;
using MongoDB.Driver;

namespace InventoryHold.Infrastructure;

public class InventoryHoldRepository : IInventoryHoldRepository
{
    private readonly IMongoCollection<InventoryItemDocument> _inventoryCollection;
    private readonly IMongoCollection<HoldRecordDocument> _holdsCollection;

    public InventoryHoldRepository(InventoryHoldDbContext dbContext)
    {
        _inventoryCollection = dbContext.Database.GetCollection<InventoryItemDocument>("inventory");
        _holdsCollection = dbContext.Database.GetCollection<HoldRecordDocument>("holds");
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(string sku, CancellationToken cancellationToken = default)
    {
        var document = await _inventoryCollection.Find(x => x.Sku == sku).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : new InventoryItem { Sku = document.Sku, Name = document.Name, AvailableQuantity = document.AvailableQuantity };
    }

    public async Task<bool> ReserveInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryCollection.UpdateOneAsync(
            x => x.Sku == sku && x.AvailableQuantity >= quantity,
            Builders<InventoryItemDocument>.Update.Inc(x => x.AvailableQuantity, -quantity),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> RestoreInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryCollection.UpdateOneAsync(
            x => x.Sku == sku,
            Builders<InventoryItemDocument>.Update.Inc(x => x.AvailableQuantity, quantity),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<HoldRecord> CreateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default)
    {
        await _holdsCollection.InsertOneAsync(new HoldRecordDocument
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            Items = hold.Items.Select(x => new HoldItemDocument { Sku = x.Sku, Quantity = x.Quantity }).ToList(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt
        }, cancellationToken);
        return hold;
    }

    public async Task<HoldRecord?> GetHoldAsync(string holdId, CancellationToken cancellationToken = default)
    {
        var document = await _holdsCollection.Find(x => x.HoldId == holdId).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : new HoldRecord
        {
            HoldId = document.HoldId,
            CustomerId = document.CustomerId,
            Status = Enum.Parse<HoldStatus>(document.Status),
            Items = document.Items.Select(x => new HoldItem { Sku = x.Sku, Quantity = x.Quantity }).ToList(),
            CreatedAt = document.CreatedAt,
            ExpiresAt = document.ExpiresAt
        };
    }

    public async Task<HoldRecord> UpdateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default)
    {
        await _holdsCollection.ReplaceOneAsync(x => x.HoldId == hold.HoldId, new HoldRecordDocument
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            Items = hold.Items.Select(x => new HoldItemDocument { Sku = x.Sku, Quantity = x.Quantity }).ToList(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt
        }, cancellationToken: cancellationToken);
        return hold;
    }

    public async Task<IReadOnlyList<InventoryItem>> ListInventoryAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _inventoryCollection.Find(_ => true).ToListAsync(cancellationToken);
        return documents.Select(x => new InventoryItem { Sku = x.Sku, Name = x.Name, AvailableQuantity = x.AvailableQuantity }).ToList();
    }

    public async Task<IReadOnlyList<HoldRecord>> ListActiveHoldsAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _holdsCollection.Find(x => x.Status == HoldStatus.Active.ToString()).ToListAsync(cancellationToken);
        return documents.Select(x => new HoldRecord { HoldId = x.HoldId, CustomerId = x.CustomerId, Status = Enum.Parse<HoldStatus>(x.Status), Items = x.Items.Select(i => new HoldItem { Sku = i.Sku, Quantity = i.Quantity }).ToList(), CreatedAt = x.CreatedAt, ExpiresAt = x.ExpiresAt }).ToList();
    }

    private sealed class InventoryItemDocument
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }

    private sealed class HoldRecordDocument
    {
        public string HoldId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<HoldItemDocument> Items { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private sealed class HoldItemDocument
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
