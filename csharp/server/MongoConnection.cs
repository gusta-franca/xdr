
using MongoDB.Bson;
using MongoDB.Driver;

public class MongoConnection
{
    public IMongoCollection<BsonDocument> Movies { get; }

    public MongoConnection(
        string mongoUri,
        string databaseName,
        string collectionName)
    {
        var client = new MongoClient(mongoUri);
        var database = client.GetDatabase(databaseName);

        Movies = database.GetCollection<BsonDocument>(
            collectionName);
    }
}
