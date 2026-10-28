
using DotNetEnv;
using System.Net;
using System.Net.Sockets;
using Mflix;
using Protocol;
using Serilog;

public class Client
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();
        try
        {
            string envPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                ".env"
            );

            if (!File.Exists(envPath))
            {
                throw new FileNotFoundException(
                    $"Ficheiro .env não encontrado em: {envPath}"
                );
            }

            Env.Load(envPath);
            Log.Information("Configuração carregada de {EnvPath}", envPath);
            bool running = true;

            while (running)
            {
                ShowMenu();
                string? option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Log.Information("Opção selecionada: criar filme.");
                        try
                        {
                            await CreateMovie();
                        }
                        catch (SocketException ex)
                        {
                            Log.Error(ex, "Falha ao conectar para criar o filme.");
                        }
                        catch (IOException ex)
                        {
                            Log.Error(ex, "Falha de comunicação ao criar o filme.");
                        }
                        break;

                    case "2":
                        Log.Information("Opção selecionada: consultar filme.");
                        await GetMovie();
                        break;

                    case "3":
                        Log.Information("Opção selecionada: atualizar filme.");
                        try
                        {
                            await UpdateMovie();
                        }
                        catch (SocketException ex)
                        {
                            Log.Error(ex, "Falha ao conectar para atualizar o filme.");
                        }
                        catch (IOException ex)
                        {
                            Log.Error(ex, "Falha de comunicação durante a atualização.");
                        }
                        break;

                    case "4":
                        Log.Information("Opção selecionada: excluir filme.");
                        try
                        {
                            await DeleteMovie();
                        }
                        catch (SocketException ex)
                        {
                            Log.Error(ex, "Não foi possível conectar para excluir o filme.");
                        }
                        catch (IOException ex)
                        {
                            Log.Error(ex, "Erro de comunicação durante a exclusão.");
                        }
                        break;

                    case "5":
                        Log.Information("Opção selecionada: pesquisar por ator.");
                        try
                        {
                            await ListMoviesByActor();
                        }
                        catch (SocketException ex)
                        {
                            Log.Error(ex, "Não foi possível conectar para pesquisar por ator.");
                        }
                        catch (IOException ex)
                        {
                            Log.Error(ex, "Erro de comunicação durante a pesquisa por ator.");
                        }
                        break;

                    case "6":
                        Log.Information("Opção selecionada: pesquisar por gênero.");
                        await ListMoviesByGenre();
                        break;

                    case "0":
                        running = false;
                        Log.Information("A encerrar o cliente...");
                        break;

                    default:
                        Log.Information("Opção inválida. Tente novamente.");
                        break;
                }
            }

        }
        catch (SocketException ex)
        {
            Log.Information(
                $"Não foi possível conectar ao servidor: {ex.Message}"
            );
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    private static void ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("====== MFlix Client ======");
        Console.WriteLine("1. Criar filme");
        Console.WriteLine("2. Consultar filme por ID");
        Console.WriteLine("3. Atualizar filme");
        Console.WriteLine("4. Excluir filme");
        Console.WriteLine("5. Listar filmes por ator");
        Console.WriteLine("6. Listar filmes por gênero");
        Console.WriteLine("0. Sair");
        Console.WriteLine("==========================");
        Console.Write("Escolha uma opção: ");
    }


    private static async Task<TcpClient> ConnectToServer()
    {
        string serverAddress =
            Environment.GetEnvironmentVariable("SERVER_ADDR")
            ?? throw new InvalidOperationException(
                "SERVER_ADDR não configurada no .env."
            );

        string[] parts = serverAddress.Split(':');

        if (parts.Length != 2 ||
            !IPAddress.TryParse(parts[0], out IPAddress? ipAddress) ||
            !int.TryParse(parts[1], out int port) ||
            port < 1 || port > 65535)
        {
            throw new InvalidOperationException(
                "SERVER_ADDR inválida."
            );
        }

        var client = new TcpClient();

        try
        {
            await client.ConnectAsync(ipAddress!, port);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task CreateMovie()
    {
        Console.Write("Título: ");
        string? title = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("O título não pode ser vazio.");
            return;
        }

        Console.Write("Ano: ");
        string? yearInput = Console.ReadLine();

        if (!int.TryParse(yearInput, out int year))
        {
            Console.WriteLine("Ano inválido.");
            return;
        }

        Console.Write("Sinopse: ");
        string? plot = Console.ReadLine();

        var movie = new Movie
        {
            Title = title,
            Year = year,
            Plot = plot ?? ""
        };

        Console.Write("Gêneros (separados por vírgula): ");
        string? genresInput = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(genresInput))
        {
            movie.Genres.AddRange(
                genresInput.Split(
                    ',',
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        }

        var request = new Request
        {
            Create = new CreateMovieRequest
            {
                Movie = movie
            }
        };

        using TcpClient client = await ConnectToServer();
        using NetworkStream stream = client.GetStream();

        await MessageProtocol.WriteMessage(stream, request);

        Response response =
            await MessageProtocol.ReadMessage(stream, Response.Parser);

        if (response.Status == Status.Ok &&
            response.PayloadCase == Response.PayloadOneofCase.Movie)
        {
            Console.WriteLine("\nFilme criado com sucesso!");
            Console.WriteLine($"ID gerado: {response.Movie.Id}");
            Console.WriteLine($"Título: {response.Movie.Title}");
        }
        else
        {
            Console.WriteLine(
                $"Erro ({response.Status}): {response.ErrorMessage}"
            );
        }
    }


    private static async Task GetMovie()
    {
        Console.Write("Digite o ID do filme: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("O ID não pode ser vazio.");
            Log.Warning("Consulta de filme cancelada: ID vazio.");
            return;
        }

        Log.Information("Iniciando consulta do filme com ID {Id}", id);

        using TcpClient client = await ConnectToServer();
        using NetworkStream stream = client.GetStream();

        var request = new Request
        {
            Get = new GetMovieRequest
            {
                Id = id
            }
        };

        Log.Information("Enviando requisição para consultar filme {Id}", id);
        await MessageProtocol.WriteMessage(stream, request);

        Response response =
            await MessageProtocol.ReadMessage(stream, Response.Parser);

        if (response.Status == Status.Ok &&
            response.PayloadCase == Response.PayloadOneofCase.Movie)
        {
            Movie movie = response.Movie;

            Log.Information(
                "Filme encontrado: {Id} - {Title}",
                movie.Id,
                movie.Title
            );

            Console.WriteLine("\n--- Filme encontrado ---");
            Console.WriteLine($"ID: {movie.Id}");
            Console.WriteLine($"Título: {movie.Title}");
            Console.WriteLine($"Ano: {movie.Year}");
            Console.WriteLine($"Gêneros: {string.Join(", ", movie.Genres)}");
            Console.WriteLine($"Diretores: {string.Join(", ", movie.Directors)}");
            Console.WriteLine($"Sinopse: {movie.Plot}");
        }
        else
        {
            Log.Warning(
                "Consulta do filme {Id} falhou. Status: {Status}. Motivo: {Error}",
                id,
                response.Status,
                response.ErrorMessage
            );

            Console.WriteLine($"Erro: {response.ErrorMessage}");

            if (response.Status == Status.NotFound)
            {
                Console.WriteLine("Nenhum filme foi encontrado com esse ID.");
            }
        }
    }

    private static async Task UpdateMovie()
    {
        Console.Write("Digite o ID do filme que deseja atualizar: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            Console.WriteLine("O ID não pode ser vazio.");
            Log.Warning("Atualização de filme cancelada: ID vazio.");
            return;
        }

        Console.Write("Novo título: ");
        string? title = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("O título não pode ser vazio.");
            Log.Warning("Atualização de filme cancelada: título vazio.");
            return;
        }

        Console.Write("Novo ano: ");
        string? yearInput = Console.ReadLine();

        if (!int.TryParse(yearInput, out int year))
        {
            Console.WriteLine("Ano inválido.");
            Log.Warning("Atualização do filme {Id} cancelada: ano inválido.", id);
            return;
        }

        Console.Write("Nova sinopse: ");
        string? plot = Console.ReadLine();

        var movie = new Movie
        {
            Id = id,
            Title = title,
            Year = year,
            Plot = plot ?? ""
        };

        Console.Write("Novos gêneros (separados por vírgula): ");
        string? genresInput = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(genresInput))
        {
            movie.Genres.AddRange(
                genresInput.Split(
                    ',',
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        }

        var request = new Request
        {
            Update = new UpdateMovieRequest
            {
                Movie = movie
            }
        };

        Log.Information("Iniciando atualização do filme {Id}", id);

        using TcpClient client = await ConnectToServer();
        using NetworkStream stream = client.GetStream();

        await MessageProtocol.WriteMessage(stream, request);

        Log.Information("Requisição de atualização enviada para o filme {Id}", id);

        Response response =
            await MessageProtocol.ReadMessage(stream, Response.Parser);

        if (response.Status == Status.Ok &&
            response.PayloadCase == Response.PayloadOneofCase.Movie)
        {
            Log.Information(
                "Filme {Id} atualizado com sucesso: {Title}",
                response.Movie.Id,
                response.Movie.Title
            );

            Console.WriteLine("Filme atualizado com sucesso!");
            Console.WriteLine($"ID: {response.Movie.Id}");
            Console.WriteLine($"Título: {response.Movie.Title}");
        }
        else
        {
            Log.Warning(
                "Falha ao atualizar filme {Id}. Status: {Status}. Motivo: {Error}",
                id,
                response.Status,
                response.ErrorMessage
            );

            Console.WriteLine(
                $"Erro ({response.Status}): {response.ErrorMessage}"
            );
        }
    }

    private static async Task DeleteMovie()
    {
        Console.Write("Digite o ID do filme que deseja excluir: ");
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            Log.Warning("Exclusão de filme cancelada: ID vazio.");
            return;
        }

        Console.Write($"Tem certeza de que deseja excluir o filme {id}? (s/n): ");
        string? confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "s", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Exclusão do filme {Id} cancelada pelo utilizador.", id);
            return;
        }

        Log.Information("Iniciando exclusão do filme {Id}", id);

        var request = new Request
        {
            Delete = new DeleteMovieRequest
            {
                Id = id
            }
        };

        using TcpClient client = await ConnectToServer();
        using NetworkStream stream = client.GetStream();

        await MessageProtocol.WriteMessage(stream, request);

        Log.Information("Requisição de exclusão enviada para o filme {Id}", id);

        Response response =
            await MessageProtocol.ReadMessage(stream, Response.Parser);

        if (response.Status == Status.Ok)
        {
            Log.Information("Filme {Id} excluído com sucesso.", id);
        }
        else
        {
            Log.Warning(
                "Falha ao excluir filme {Id}. Status: {Status}. Motivo: {Error}",
                id,
                response.Status,
                response.ErrorMessage
            );
        }
    }

    private static async Task ListMoviesByActor()
    {
        Console.Write("Digite o nome do ator: ");
        string? actor = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(actor))
        {
            Log.Warning("Pesquisa por ator cancelada: nome vazio.");
            return;
        }

        Log.Information("Pesquisando filmes do ator {Actor}", actor);

        var request = new Request
        {
            ListByActor = new ListByActorRequest
            {
                Actor = actor
            }
        };

        using TcpClient client = await ConnectToServer();
        using NetworkStream stream = client.GetStream();

        await MessageProtocol.WriteMessage(stream, request);

        Log.Information("Requisição de pesquisa por ator enviada: {Actor}", actor);

        Response response =
            await MessageProtocol.ReadMessage(stream, Response.Parser);

        if (response.Status == Status.Ok &&
            response.PayloadCase == Response.PayloadOneofCase.Movies)
        {
            var movies = response.Movies.Movies;

            Log.Information(
                "Pesquisa por ator {Actor} concluída. Filmes encontrados: {Count}",
                actor,
                movies.Count
            );

            if (movies.Count == 0)
            {
                Console.WriteLine("Nenhum filme encontrado para esse ator.");
                return;
            }

            Console.WriteLine($"\n--- Filmes com {actor} ---");

            foreach (Movie movie in movies)
            {
                Console.WriteLine("----------------------------");
                Console.WriteLine($"ID: {movie.Id}");
                Console.WriteLine($"Título: {movie.Title}");
                Console.WriteLine($"Ano: {movie.Year}");
                Console.WriteLine($"Gêneros: {string.Join(", ", movie.Genres)}");
            }

            Console.WriteLine("----------------------------");
            Console.WriteLine($"Total de filmes: {movies.Count}");
        }
        else
        {
            Log.Warning(
                "Falha na pesquisa por ator {Actor}. Status: {Status}. Motivo: {Error}",
                actor,
                response.Status,
                response.ErrorMessage
            );
        }
    }


    private static async Task ListMoviesByGenre()
    {
        Console.Write("Digite o gênero dos filmes: ");
        string? genre = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(genre))
        {
            Log.Warning("Pesquisa por gênero cancelada: gênero vazio.");
            Console.WriteLine("O gênero não pode ser vazio.");
            return;
        }

        Log.Information(
            "Iniciando pesquisa de filmes pelo gênero {Genre}",
            genre
        );

        var request = new Request
        {
            ListByGenre = new ListByGenreRequest
            {
                Genre = genre
            }
        };

        try
        {
            using TcpClient client = await ConnectToServer();
            using NetworkStream stream = client.GetStream();

            Log.Information("Conexão estabelecida para pesquisa por gênero {Genre}", genre);

            await MessageProtocol.WriteMessage(stream, request);

            Log.Information("Requisição enviada para pesquisar o gênero {Genre}", genre);

            Response response =
                await MessageProtocol.ReadMessage(stream, Response.Parser);

            Log.Information(
                "Resposta recebida para o gênero {Genre}: {Status}",
                genre,
                response.Status
            );

            if (response.Status == Status.Ok &&
                response.PayloadCase == Response.PayloadOneofCase.Movies)
            {
                var movies = response.Movies.Movies;

                Log.Information(
                    "Pesquisa pelo gênero {Genre} concluída. Total de filmes: {Count}",
                    genre,
                    movies.Count
                );

                if (movies.Count == 0)
                {
                    Log.Information(
                        "Nenhum filme encontrado para o gênero {Genre}",
                        genre
                    );

                    Console.WriteLine("Nenhum filme encontrado para esse gênero.");
                    return;
                }

                Console.WriteLine($"\n--- Filmes do gênero {genre} ---");

                foreach (Movie movie in movies)
                {
                    Console.WriteLine("----------------------------");
                    Console.WriteLine($"ID: {movie.Id}");
                    Console.WriteLine($"Título: {movie.Title}");
                    Console.WriteLine($"Ano: {movie.Year}");
                    Console.WriteLine($"Gêneros: {string.Join(", ", movie.Genres)}");

                    Log.Information(
                        "Filme listado: {Id} - {Title} ({Year})",
                        movie.Id,
                        movie.Title,
                        movie.Year
                    );
                }

                Console.WriteLine("----------------------------");
                Console.WriteLine($"Total de filmes: {movies.Count}");
            }
            else
            {
                Log.Warning(
                    "Pesquisa por gênero {Genre} falhou. Status: {Status}. Motivo: {Error}",
                    genre,
                    response.Status,
                    response.ErrorMessage
                );

                Console.WriteLine(
                    $"Erro ({response.Status}): {response.ErrorMessage}"
                );
            }
        }
        catch (SocketException ex)
        {
            Log.Error(
                ex,
                "Falha ao conectar ao servidor durante a pesquisa por gênero {Genre}",
                genre
            );

            Console.WriteLine("Não foi possível conectar ao servidor.");
        }
        catch (IOException ex)
        {
            Log.Error(
                ex,
                "Erro de comunicação durante a pesquisa por gênero {Genre}",
                genre
            );

            Console.WriteLine("Ocorreu um erro na comunicação com o servidor.");
        }
    }

}
