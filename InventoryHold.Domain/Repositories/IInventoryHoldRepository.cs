namespace InventoryHold.Domain.Repositories;

public interface IInventoryHoldRepository
{
    Task<InventoryItem?> GetInventoryItemAsync(string sku, CancellationToken cancellationToken = default);
    Task<bool> ReserveInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RestoreInventoryAsync(string sku, int quantity, CancellationToken cancellationToken = default);
    Task<HoldRecord> CreateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default);
    Task<HoldRecord?> GetHoldAsync(string holdId, CancellationToken cancellationToken = default);
    Task<HoldRecord> UpdateHoldAsync(HoldRecord hold, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> ListInventoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HoldRecord>> ListActiveHoldsAsync(CancellationToken cancellationToken = default);
}
