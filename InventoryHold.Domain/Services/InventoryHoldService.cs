using InventoryHold.Contracts;
using InventoryHold.Domain.Repositories;

namespace InventoryHold.Domain.Services;

public class InventoryHoldService
{
    private readonly IInventoryHoldRepository _repository;
    private readonly ICacheService _cache;
    private readonly IEventPublisher _publisher;
    private readonly InventoryHoldOptions _options;

    public InventoryHoldService(
        IInventoryHoldRepository repository,
        ICacheService cache,
        IEventPublisher publisher,
        InventoryHoldOptions options)
    {
        _repository = repository;
        _cache = cache;
        _publisher = publisher;
        _options = options;
    }

    public async Task<CreateHoldResponse> CreateHoldAsync(CreateHoldRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new InventoryHoldDomainException("Hold request must contain at least one item.");
        }

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InventoryHoldDomainException($"Quantity for SKU {item.Sku} must be greater than zero.");
            }
        }

        var holdItems = new List<HoldItem>();
        foreach (var item in request.Items)
        {
            var inventory = await _repository.GetInventoryItemAsync(item.Sku, cancellationToken);
            if (inventory is null)
            {
                throw new InventoryHoldDomainException($"Inventory item for SKU {item.Sku} was not found.");
            }

            if (inventory.AvailableQuantity < item.Quantity)
            {
                throw new InventoryHoldDomainException($"Insufficient stock for SKU {item.Sku}.");
            }

            var reserved = await _repository.ReserveInventoryAsync(item.Sku, item.Quantity, cancellationToken);
            if (!reserved)
            {
                throw new InventoryHoldDomainException($"Insufficient stock for SKU {item.Sku}.");
            }

            holdItems.Add(new HoldItem { Sku = item.Sku, Quantity = item.Quantity });
        }

        var hold = new HoldRecord
        {
            HoldId = Guid.NewGuid().ToString("N"),
            CustomerId = request.CustomerId,
            Status = HoldStatus.Active,
            Items = holdItems,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.DefaultHoldMinutes)
        };

        await _repository.CreateHoldAsync(hold, cancellationToken);
        await _cache.RemoveAsync("inventory:all", cancellationToken);
        await _publisher.PublishAsync("HoldCreated", new { hold.HoldId, hold.CustomerId, hold.Items, hold.ExpiresAt }, cancellationToken);

        return new CreateHoldResponse
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            ExpiresAt = hold.ExpiresAt,
            Items = hold.Items.Select(x => new HoldItemResponse { Sku = x.Sku, Quantity = x.Quantity }).ToList()
        };
    }

    public async Task<HoldResponse> GetHoldByIdAsync(string holdId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"hold:{holdId}";
        var cached = await _cache.GetAsync<HoldResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var hold = await _repository.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            throw new InventoryHoldDomainException("Hold not found.");
        }

        if (hold.Status == HoldStatus.Active && hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;
            await _repository.UpdateHoldAsync(hold, cancellationToken);
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            await _publisher.PublishAsync("HoldExpired", new { hold.HoldId, hold.CustomerId, hold.Items, hold.ExpiresAt }, cancellationToken);
            throw new InventoryHoldDomainException("Hold not found.");
        }

        var response = new HoldResponse
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            ExpiresAt = hold.ExpiresAt,
            Items = hold.Items.Select(x => new HoldItemResponse { Sku = x.Sku, Quantity = x.Quantity }).ToList()
        };

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);
        return response;
    }

    public async Task<HoldResponse> ReleaseHoldAsync(string holdId, CancellationToken cancellationToken = default)
    {
        var hold = await _repository.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            throw new InventoryHoldDomainException("Hold not found.");
        }

        if (hold.Status == HoldStatus.Released)
        {
            throw new InventoryHoldDomainException("Hold is already released.");
        }

        if (hold.Status == HoldStatus.Active && hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;
            await _repository.UpdateHoldAsync(hold, cancellationToken);
            await _publisher.PublishAsync("HoldExpired", new { hold.HoldId, hold.CustomerId, hold.Items, hold.ExpiresAt }, cancellationToken);
            throw new InventoryHoldDomainException("Hold not found.");
        }

        foreach (var item in hold.Items)
        {
            await _repository.RestoreInventoryAsync(item.Sku, item.Quantity, cancellationToken);
        }

        hold.Status = HoldStatus.Released;
        await _repository.UpdateHoldAsync(hold, cancellationToken);
        await _cache.RemoveAsync($"hold:{holdId}", cancellationToken);
        await _cache.RemoveAsync("inventory:all", cancellationToken);
        await _publisher.PublishAsync("HoldReleased", new { hold.HoldId, hold.CustomerId, hold.Items, hold.ExpiresAt }, cancellationToken);

        return new HoldResponse
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            ExpiresAt = hold.ExpiresAt,
            Items = hold.Items.Select(x => new HoldItemResponse { Sku = x.Sku, Quantity = x.Quantity }).ToList()
        };
    }

    public async Task<IReadOnlyList<HoldResponse>> GetActiveHoldsAsync(CancellationToken cancellationToken = default)
    {
        var holds = await _repository.ListActiveHoldsAsync(cancellationToken);
        return holds.Select(hold => new HoldResponse
        {
            HoldId = hold.HoldId,
            CustomerId = hold.CustomerId,
            Status = hold.Status.ToString(),
            ExpiresAt = hold.ExpiresAt,
            Items = hold.Items.Select(x => new HoldItemResponse { Sku = x.Sku, Quantity = x.Quantity }).ToList()
        }).ToList();
    }

    public async Task<InventoryListResponse> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "inventory:all";
        var cached = await _cache.GetAsync<InventoryListResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var inventoryItems = await _repository.ListInventoryAsync(cancellationToken);
        var response = new InventoryListResponse
        {
            Items = inventoryItems.Select(x => new InventoryItemResponse { Sku = x.Sku, Name = x.Name, AvailableQuantity = x.AvailableQuantity }).ToList()
        };

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(2), cancellationToken);
        return response;
    }
}
