using InventoryHold.Contracts;
using InventoryHold.Domain;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using Moq;

namespace InventoryHold.UnitTests;

public class InventoryHoldServiceTests
{
    [Test]
    public void CreateHoldAsync_WhenRequestHasNoItems_ThrowsDomainException()
    {
        var repository = new Mock<IInventoryHoldRepository>();
        var cache = new Mock<ICacheService>();
        var publisher = new Mock<IEventPublisher>();
        var service = new InventoryHoldService(repository.Object, cache.Object, publisher.Object, new InventoryHoldOptions { DefaultHoldMinutes = 15 });

        var ex = Assert.ThrowsAsync<InventoryHoldDomainException>(() => service.CreateHoldAsync(new CreateHoldRequest
        {
            CustomerId = "cust-1",
            Items = []
        }));

        Assert.That(ex!.Message, Does.Contain("at least one item"));
    }

    [Test]
    public async Task CreateHoldAsync_WhenInventoryIsInsufficient_ThrowsDomainException()
    {
        var repository = new Mock<IInventoryHoldRepository>();
        repository.Setup(r => r.GetInventoryItemAsync("sku-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItem { Sku = "sku-1", Name = "Widget", AvailableQuantity = 1 });
        repository.Setup(r => r.ReserveInventoryAsync("sku-1", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cache = new Mock<ICacheService>();
        var publisher = new Mock<IEventPublisher>();
        var service = new InventoryHoldService(repository.Object, cache.Object, publisher.Object, new InventoryHoldOptions { DefaultHoldMinutes = 15 });

        var ex = Assert.ThrowsAsync<InventoryHoldDomainException>(() => service.CreateHoldAsync(new CreateHoldRequest
        {
            CustomerId = "cust-1",
            Items = [new HoldItemRequest { Sku = "sku-1", Quantity = 2 }]
        }));

        Assert.That(ex!.Message, Does.Contain("Insufficient stock"));
        publisher.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateHoldAsync_WhenStockIsAvailable_CreatesHoldAndPublishesEvent()
    {
        var repository = new Mock<IInventoryHoldRepository>();
        repository.Setup(r => r.GetInventoryItemAsync("sku-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItem { Sku = "sku-1", Name = "Widget", AvailableQuantity = 5 });
        repository.Setup(r => r.ReserveInventoryAsync("sku-1", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.CreateHoldAsync(It.IsAny<HoldRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HoldRecord hold, CancellationToken _) => hold);

        var cache = new Mock<ICacheService>();
        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new InventoryHoldService(repository.Object, cache.Object, publisher.Object, new InventoryHoldOptions { DefaultHoldMinutes = 15 });

        var result = await service.CreateHoldAsync(new CreateHoldRequest
        {
            CustomerId = "cust-1",
            Items = [new HoldItemRequest { Sku = "sku-1", Quantity = 2 }]
        });

        Assert.That(result.HoldId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Items[0].Quantity, Is.EqualTo(2));
        publisher.Verify(p => p.PublishAsync("HoldCreated", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ReleaseHoldAsync_WhenHoldIsAlreadyReleased_ThrowsDomainException()
    {
        var repository = new Mock<IInventoryHoldRepository>();
        repository.Setup(r => r.GetHoldAsync("hold-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldRecord { HoldId = "hold-1", Status = HoldStatus.Released });

        var cache = new Mock<ICacheService>();
        var publisher = new Mock<IEventPublisher>();
        var service = new InventoryHoldService(repository.Object, cache.Object, publisher.Object, new InventoryHoldOptions { DefaultHoldMinutes = 15 });

        var ex = Assert.ThrowsAsync<InventoryHoldDomainException>(() => service.ReleaseHoldAsync("hold-1"));

        Assert.That(ex!.Message, Does.Contain("already released"));
    }

    [Test]
    public void GetHoldByIdAsync_WhenHoldIsExpired_ThrowsNotFoundAndPublishesEvent()
    {
        var repository = new Mock<IInventoryHoldRepository>();
        repository.Setup(r => r.GetHoldAsync("hold-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldRecord
            {
                HoldId = "hold-2",
                Status = HoldStatus.Active,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
        repository.Setup(r => r.UpdateHoldAsync(It.IsAny<HoldRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HoldRecord hold, CancellationToken _) => hold);
        repository.Setup(r => r.RestoreInventoryAsync("", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cache = new Mock<ICacheService>();
        var publisher = new Mock<IEventPublisher>();
        var service = new InventoryHoldService(repository.Object, cache.Object, publisher.Object, new InventoryHoldOptions { DefaultHoldMinutes = 15 });

        var ex = Assert.ThrowsAsync<InventoryHoldDomainException>(() => service.GetHoldByIdAsync("hold-2"));

        Assert.That(ex!.Message, Does.Contain("not found"));
        publisher.Verify(p => p.PublishAsync("HoldExpired", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
