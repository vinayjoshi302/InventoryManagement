using InventoryHold.Domain;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using InventoryHold.Infrastructure;
using InventoryHold.Infrastructure.Caching;
using InventoryHold.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<InventoryHoldOptions>(builder.Configuration.GetSection("InventoryHold"));
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IOptions<InventoryHoldOptions>>().Value;
    return config;
});

builder.Services.AddSingleton(sp =>
{
    var mongoSection = builder.Configuration.GetSection("MongoDb");
    return new InventoryHoldDbContext(mongoSection["ConnectionString"]!, mongoSection["DatabaseName"]!);
});

builder.Services.AddSingleton<IInventoryHoldRepository>(_ =>
{
    var mongoSection = builder.Configuration.GetSection("MongoDb");
    var connectionString = mongoSection["ConnectionString"];
    var databaseName = mongoSection["DatabaseName"];

    if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(databaseName))
    {
        return new InventoryHoldInMemoryRepository();
    }

    try
    {
        var client = new MongoClient(connectionString);
        client.GetDatabase(databaseName).RunCommand<BsonDocument>(new BsonDocument("ping", 1));
        return new InventoryHoldRepository(new InventoryHoldDbContext(connectionString, databaseName));
    }
    catch
    {
        return new InventoryHoldInMemoryRepository();
    }
});

builder.Services.AddSingleton<ICacheService>(_ =>
{
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        return new InMemoryCacheService();
    }

    try
    {
        return new RedisCacheService(StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
    }
    catch
    {
        return new InMemoryCacheService();
    }
});

builder.Services.AddSingleton<IEventPublisher>(_ =>
{
    var rabbitSection = builder.Configuration.GetSection("RabbitMq");
    try
    {
        return new RabbitMqEventPublisher(
            rabbitSection["HostName"]!,
            rabbitSection["UserName"]!,
            rabbitSection["Password"]!,
            rabbitSection["VirtualHost"]!);
    }
    catch
    {
        return new NoOpEventPublisher();
    }
});
builder.Services.AddSingleton<InventoryHoldService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.MapControllers();

await SeedInventoryAsync(app.Services);

app.Run();

static async Task SeedInventoryAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IInventoryHoldRepository>();

    try
    {
        var inventory = await repository.ListInventoryAsync();
        if (inventory.Count == 0)
        {
            var inMemory = repository as InventoryHoldInMemoryRepository;
            inMemory?.SeedDefaultInventory();
        }
    }
    catch
    {
        var inMemory = repository as InventoryHoldInMemoryRepository;
        inMemory?.SeedDefaultInventory();
    }
}

internal sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _values[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.Remove(key);
        return Task.CompletedTask;
    }
}

internal sealed class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
