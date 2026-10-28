/*
    Data de criação: 28/10/2026
    Aluna: Caroline Marques Lau
    Arquivo do para conexão com o banco
*/

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
