
using MongoDB.Bson;
using MongoDB.Driver;
using Mflix;

public class MovieRepository(MongoConnection connection)
{
    private readonly IMongoCollection<BsonDocument> _movies = connection.Movies;

    public async Task<Movie?> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out ObjectId objectId))
            return null;

        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", objectId);

        var document = await _movies.Find(filter).FirstOrDefaultAsync();

        if (document is null)
            return null;

        return ToMovie(document);
    }


    private static Movie ToMovie(BsonDocument document)
    {
        var movie = new Movie
        {
            Id = document["_id"].AsObjectId.ToString(),
            Title = document.GetValue("title", "").AsString,
            Year = document.GetValue("year", 0).ToInt32(),
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

    private static IEnumerable<string> ReadStringArray(
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


    public async Task<Movie> Create(Movie movie)
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


    private static BsonDocument ToDocument(Movie movie)
    {
        return new BsonDocument
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
    }


    public async Task<Movie?> UpdateById(Movie movie)
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


    public async Task<bool> DeleteById(string id)
    {
        if (!ObjectId.TryParse(id, out ObjectId objectId))
            return false;

        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", objectId);

        var result = await _movies.DeleteOneAsync(filter);

        return result.DeletedCount > 0;
    }


    public async Task<List<Movie>> ListByActor(string actor)
    {
        var filter = Builders<BsonDocument>.Filter
            .Eq("cast", actor);

        var documents = await _movies.Find(filter).ToListAsync();

        return documents.Select(ToMovie).ToList();
    }

    public async Task<List<Movie>> ListByGenre(string genre)
    {
        var filter = Builders<BsonDocument>.Filter
            .Eq("genres", genre);

        var documents = await _movies.Find(filter).ToListAsync();

        return documents.Select(ToMovie).ToList();
    }

}
