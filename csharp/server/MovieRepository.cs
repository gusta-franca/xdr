/*
   Data de criação: 28/10/2026
   Aluna: Caroline Marques Lau
   Arquivo de operações no banco
*/

using MongoDB.Bson;
using MongoDB.Driver;
using Mflix;
using System.Text.RegularExpressions;

public class MovieRepository(MongoConnection connection)
{
    private readonly IMongoCollection<BsonDocument> _movies = connection.Movies; // Binary JSON

    public async Task<Movie?> GetById(string id) // Obtém um filme pelo seu ID
    {
        if (!ObjectId.TryParse(id, out ObjectId objectId))
            return null;

        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", objectId); // Cria o filtro para id igual ao informado

        var document = await _movies.Find(filter).FirstOrDefaultAsync();

        if (document is null)
            return null;

        return ToMovie(document);
    }

    private static int ReadYear(BsonDocument document)
    {
        if (!document.TryGetValue("year", out var value))
            return 0;

        if (value.IsInt32)
            return value.AsInt32;

        if (value.IsString &&
            int.TryParse(value.AsString, out int year))
        {
            return year;
        }

        return 0;
    }
    private static Movie ToMovie(BsonDocument document) // Converte o Binary JSON no tipo Movie
    {
        var movie = new Movie
        {
            Id = document["_id"].AsObjectId.ToString(),
            Title = document.GetValue("title", "").AsString,
            Year = ReadYear(document),
            Plot = document.GetValue("plot", "").AsString,
            Fullplot = document.GetValue("fullplot", "").AsString,
            Runtime = document.GetValue("runtime", 0).ToInt32(),
            Rated = document.GetValue("rated", "").AsString,
            Type = document.GetValue("type", "").AsString,
            Released = document.GetValue("released", "").ToString(),
            Lastupdated = document.GetValue("lastupdated", "").ToString()
        };

        movie.Genres.AddRange(ReadStringArray(document, "genres"));
        movie.Cast.AddRange(ReadStringArray(document, "cast"));
        movie.Countries.AddRange(ReadStringArray(document, "countries"));
        movie.Directors.AddRange(ReadStringArray(document, "directors"));

        return movie;
    }

    private static IEnumerable<string> ReadStringArray( //Ler um campo do MongoDB com listas de textos
        BsonDocument document,
        string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) ||
            !value.IsBsonArray)
        {
            return [];
        }

        return value.AsBsonArray
            .Where(item => item.IsString)
            .Select(item => item.AsString);
    }

    public async Task<Movie> Create(Movie movie) // Método para criar um novo registro de Movie no banco
    {
        var document = new BsonDocument
        {
            { "plot", movie.Plot },
            { "genres", new BsonArray(movie.Genres) },
            { "runtime", movie.Runtime },
            { "cast", new BsonArray(movie.Cast) },
            { "num_mflix_comments", movie.NumMflixComments },
            { "title", movie.Title },
            { "fullplot", movie.Fullplot },
            { "countries", new BsonArray(movie.Countries) },
            { "released", movie.Released },
            { "directors", new BsonArray(movie.Directors) },
            { "rated", movie.Rated },
            { "lastupdated", movie.Lastupdated },
            { "year", movie.Year },
            { "type", movie.Type }
        };

        await _movies.InsertOneAsync(document);

        movie.Id = document["_id"].AsObjectId.ToString();

        return movie;
    }

    public async Task<Movie?> UpdateById(Movie movie) // Método utilizado para atualizar um filme no banco
    {
        if (!ObjectId.TryParse(movie.Id, out ObjectId objectId))
            return null;

        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", objectId);

        var update = Builders<BsonDocument>.Update
            .Set("plot", movie.Plot)
            .Set("genres", new BsonArray(movie.Genres))
            .Set("runtime", movie.Runtime)
            .Set("cast", new BsonArray(movie.Cast))
            .Set("num_mflix_comments", movie.NumMflixComments)
            .Set("title", movie.Title)
            .Set("fullplot", movie.Fullplot)
            .Set("countries", new BsonArray(movie.Countries))
            .Set("released", movie.Released)
            .Set("directors", new BsonArray(movie.Directors))
            .Set("rated", movie.Rated)
            .Set("lastupdated", movie.Lastupdated)
            .Set("year", movie.Year)
            .Set("type", movie.Type);

        var result = await _movies.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
            return null;

        return movie;
    }

    public async Task<bool> DeleteById(string id) // Método para deletar um filme pelo seu ID
    {
        if (!ObjectId.TryParse(id, out ObjectId objectId))
            return false;

        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", objectId);

        var result = await _movies.DeleteOneAsync(filter);

        return result.DeletedCount > 0;
    }

    public async Task<List<Movie>> ListByActor(string actor) // Método para listar filmes baseado em um ator
    {
        var filter = Builders<BsonDocument>.Filter
            .Regex("cast", new BsonRegularExpression( // Para desconsiderar maiúsculas e minúsculas
                $"^{Regex.Escape(actor)}$", "i"));

        var documents = await _movies.Find(filter).ToListAsync();

        return [.. documents.Select(ToMovie)];
    }

    public async Task<List<Movie>> ListByGenre(string genre) // Método para listar filmes baseado no gênero
    {
        var filter = Builders<BsonDocument>.Filter
            .Regex("genres", new BsonRegularExpression( // Para desconsiderar maiúsculas e minúsculas
                $"^{Regex.Escape(genre)}$", "i"));

        var documents = await _movies.Find(filter).ToListAsync();

        return [.. documents.Select(ToMovie)];
    }

}
