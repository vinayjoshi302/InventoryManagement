using System.Text;
using System.Text.Json;
using InventoryHold.Domain.Repositories;
using RabbitMQ.Client;

namespace InventoryHold.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(string hostName, string userName, string password, string virtualHost = "/")
    {
        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password, VirtualHost = virtualHost };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("inventory-hold-events", ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        _channel.BasicPublish("inventory-hold-events", $"hold.{eventName.ToLowerInvariant()}", null, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
