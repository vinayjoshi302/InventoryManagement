namespace InventoryHold.Domain.Repositories;

public interface IEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}
