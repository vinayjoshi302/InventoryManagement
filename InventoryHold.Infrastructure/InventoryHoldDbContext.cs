using MongoDB.Driver;

namespace InventoryHold.Infrastructure;

public class InventoryHoldDbContext
{
    public InventoryHoldDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        Database = client.GetDatabase(databaseName);
    }

    public IMongoDatabase Database { get; }
}
