using System.Net;
using System.Net.Sockets;
using Mflix;
using Serilog;
using Protocol;
using MongoDB.Bson;

public class Server
{
    public static void Main()
    {
        DotNetEnv.Env.Load();
        string mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI")
        ?? throw new InvalidOperationException("MONGODB_URI não configurada.");

        string databaseName = Environment.GetEnvironmentVariable("MONGODB_DB")
            ?? throw new InvalidOperationException("MONGODB_DB não configurada.");

        string collectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION")
            ?? throw new InvalidOperationException("MONGODB_COLLECTION não configurada.");

        Log.Logger = new LoggerConfiguration()
        // para diferenciar os clientes
        .WriteTo.Console()
        .CreateLogger();

        string serverAddress = Environment.GetEnvironmentVariable("SERVER_ADDR")
        ?? throw new InvalidOperationException("SERVER_ADDR não configurada.");

        string[] addressParts = serverAddress.Split(':');

        if (addressParts.Length != 2 ||
            !IPAddress.TryParse(addressParts[0], out IPAddress? ipAddress) ||
            !int.TryParse(addressParts[1], out int port) ||
            port < 1 || port > 65535)
        {
            Log.Error("SERVER_ADDR inválida: {Address}", serverAddress);
            return;
        }

        TcpListener server = new(ipAddress!, port);
        server.Start();
        Log.Information("Servidor iniciado na porta {port}", port);

        MongoConnection mongo = new(
        mongoUri,
        databaseName,
        collectionName);

        MovieRepository repository = new(mongo);

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Task.Run(() => HandleClient(client, repository));
        }
    }
    static async Task HandleClient(TcpClient client, MovieRepository repository)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                Log.Information("Cliente conectado");

                Request request = await MessageProtocol.ReadMessage(stream, Request.Parser);
                Log.Information("Requisição recebida: {@Request}", request);
                Response response;

                switch (request.CommandCase)
                {
                    case Request.CommandOneofCase.Create:
                        Movie movieCreate = request.Create.Movie;

                        if (movieCreate is null || string.IsNullOrWhiteSpace(movieCreate.Title))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "O filme precisa ter um título."
                            };

                            break;
                        }

                        Movie createdMovie = await repository.Create(movieCreate);

                        response = new Response
                        {
                            Status = Status.Ok,
                            Movie = createdMovie
                        };

                        Log.Information(
                            "Filme criado com ID {Id}",
                            createdMovie.Id);

                        break;

                    case Request.CommandOneofCase.Get:
                        Log.Information("Comando recebido: consultar filme");
                        string id = request.Get.Id;
                        if (!ObjectId.TryParse(id, out _))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "ID do filme inválido."
                            };

                            break;
                        }
                        Log.Information("Consultando filme com ID {Id}", id);

                        Movie? movieGet = await repository.GetById(id);

                        if (movieGet is null)
                        {
                            response = new Response
                            {
                                Status = Status.NotFound,
                                ErrorMessage = "Filme não encontrado."
                            };
                        }
                        else
                        {
                            response = new Response
                            {
                                Status = Status.Ok,
                                Movie = movieGet
                            };
                        }

                        break;

                    case Request.CommandOneofCase.Update:
                        Log.Information("Comando recebido: atualizar filme");
                        Movie movie = request.Update.Movie;

                        if (movie is null ||
                            string.IsNullOrWhiteSpace(movie.Title) ||
                            string.IsNullOrWhiteSpace(movie.Id))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "O filme precisa ter ID e título."
                            };

                            break;
                        }

                        Movie? updatedMovie = await repository.UpdateById(movie);

                        if (updatedMovie is null)
                        {
                            response = new Response
                            {
                                Status = Status.NotFound,
                                ErrorMessage = "Filme não encontrado."
                            };
                        }
                        else
                        {
                            response = new Response
                            {
                                Status = Status.Ok,
                                Movie = updatedMovie
                            };
                        }

                        break;

                    case Request.CommandOneofCase.Delete:
                        Log.Information("Comando recebido: excluir filme");
                        string idDelete = request.Delete.Id;

                        if (!ObjectId.TryParse(idDelete, out _))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "ID do filme inválido."
                            };

                            break;
                        }

                        bool deleted = await repository.DeleteById(idDelete);

                        if (!deleted)
                        {
                            response = new Response
                            {
                                Status = Status.NotFound,
                                ErrorMessage = "Filme não encontrado."
                            };
                        }
                        else
                        {
                            response = new Response
                            {
                                Status = Status.Ok
                            };
                        }

                        break;

                    case Request.CommandOneofCase.ListByActor:
                        Log.Information("Comando recebido: listar filmes por ator");
                        string actor = request.ListByActor.Actor;

                        if (string.IsNullOrWhiteSpace(actor))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "O nome do ator não pode ser vazio."
                            };

                            break;
                        }

                        List<Movie> moviesActor = await repository.ListByActor(actor);

                        response = new Response
                        {
                            Status = Status.Ok,
                            Movies = new MovieList()
                        };

                        response.Movies.Movies.AddRange(moviesActor);

                        break;

                    case Request.CommandOneofCase.ListByGenre:
                        Log.Information("Comando recebido: listar filmes por gênero");
                        string genre = request.ListByGenre.Genre;

                        if (string.IsNullOrWhiteSpace(genre))
                        {
                            response = new Response
                            {
                                Status = Status.InvalidArgument,
                                ErrorMessage = "O gênero não pode ser vazio."
                            };

                            break;
                        }

                        List<Movie> moviesGenre = await repository.ListByGenre(genre);

                        response = new Response
                        {
                            Status = Status.Ok,
                            Movies = new MovieList()
                        };

                        response.Movies.Movies.AddRange(moviesGenre);

                        break;

                    default:
                        response = new Response
                        {
                            Status = Status.InvalidArgument,
                            ErrorMessage = "Nenhum comando válido foi informado."
                        };
                        break;
                }
                await MessageProtocol.WriteMessage(stream, response);
            }
            catch (Exception ex)
            {

                Log.Error(ex, "Erro ao processar a requisição");

                try
                {
                    Response errorResponse = new()
                    {
                        Status = Status.InternalError,
                        ErrorMessage = "Erro interno ao processar a requisição."
                    };

                    await MessageProtocol.WriteMessage(stream, errorResponse);
                }
                catch (Exception sendEx)
                {
                    Log.Error(sendEx, "Não foi possível enviar a resposta de erro");
                }
            }
        }
    }
}