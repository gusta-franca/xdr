/*
   Cliente do gerenciador de filmes (Mflix) via TCP com Protocol Buffers.
*/

package client;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.util.ArrayList;
import java.util.List;
import java.util.Scanner;

import common.Config;
import common.Style;
import mflix.CreateMovieRequest;
import mflix.DeleteMovieRequest;
import mflix.GetMovieRequest;
import mflix.ListByActorRequest;
import mflix.ListByGenreRequest;
import mflix.Movie;
import mflix.MovieList;
import mflix.Request;
import mflix.Response;
import mflix.Status;
import mflix.UpdateMovieRequest;

public class Client {

    private static Scanner reader = new Scanner(System.in);

    public static void main(String[] args) {
        Config config = new Config(".env");

        try (Socket socket = new Socket(config.getServerHost(), config.getServerPort());
             InputStream in = socket.getInputStream();
             OutputStream out = socket.getOutputStream()) {

            Style.showSuccess("Conectado ao servidor");
            showMenu();

            while (true) {
                String line = ask("> ");
                if (line.isEmpty()) {
                    continue;
                }

                String[] parts = line.split("\\s+", 2);
                String command = parts[0].toUpperCase();
                String argument = "";
                if (parts.length > 1) {
                    argument = parts[1].trim();
                }

                if (command.equals("EXIT")) {
                    break;
                }

                Request request = buildRequest(command, argument);
                if (request == null) {
                    continue;
                }

                request.writeDelimitedTo(out);
                out.flush();

                Response response = Response.parseDelimitedFrom(in);
                if (response == null) {
                    Style.showError("O servidor foi desconectado.");
                    break;
                }
                showResponse(response);
            }
        } catch (IOException e) {
            Style.showError(e.getMessage());
        }
    }

    private static void showMenu() {
        Style.showInfo("Comandos: CREATE, GET <id>, UPDATE <id>, DELETE <id>, ACTOR <nome>, GENRE <categoria>, EXIT");
    }

    /** Mostra o prompt e lê uma linha do teclado. */
    private static String ask(String text) {
        Style.currentPrompt = text;
        System.out.print(text);
        System.out.flush();
        String line = reader.nextLine().trim();
        Style.currentPrompt = "";
        return line;
    }

    /** Monta o Request do comando. O argumento (id, ator ou categoria) vem da mesma linha. */
    private static Request buildRequest(String command, String argument) {
        Request.Builder builder = Request.newBuilder();

        if (command.equals("CREATE")) {
            Movie movie = readMovie("");
            builder.setCreate(CreateMovieRequest.newBuilder().setMovie(movie));
        } else if (command.equals("GET")) {
            builder.setGet(GetMovieRequest.newBuilder().setId(argument));
        } else if (command.equals("UPDATE")) {
            String id = argument;
            if (id.isEmpty()) {
                id = ask("Id do filme: ");
            }
            Movie movie = readMovie(id);
            builder.setUpdate(UpdateMovieRequest.newBuilder().setMovie(movie));
        } else if (command.equals("DELETE")) {
            builder.setDelete(DeleteMovieRequest.newBuilder().setId(argument));
        } else if (command.equals("ACTOR")) {
            builder.setListByActor(ListByActorRequest.newBuilder().setActor(argument));
        } else if (command.equals("GENRE")) {
            builder.setListByGenre(ListByGenreRequest.newBuilder().setGenre(argument));
        } else {
            Style.showError("Comando inválido!");
            return null;
        }
        return builder.build();
    }

    /** Lê os campos de um filme. Listas são separadas por vírgula. */
    private static Movie readMovie(String id) {
        Movie.Builder movie = Movie.newBuilder();
        movie.setId(id);
        movie.setTitle(ask("Título: "));
        movie.setPlot(ask("Sinopse: "));
        movie.setFullplot(ask("Sinopse completa: "));
        movie.setRated(ask("Classificação: "));
        movie.setType(ask("Tipo (ex.: movie): "));
        movie.setReleased(ask("Lançamento: "));
        movie.setRuntime(readInt("Duração (min): "));
        movie.setYear(readInt("Ano: "));
        movie.addAllGenres(splitList(ask("Categorias (separe por vírgula): ")));
        movie.addAllCast(splitList(ask("Elenco (separe por vírgula): ")));
        movie.addAllDirectors(splitList(ask("Diretores (separe por vírgula): ")));
        movie.addAllCountries(splitList(ask("Países (separe por vírgula): ")));
        return movie.build();
    }

    /** Separa o texto por vírgula, remove espaços e ignora itens vazios. */
    private static List<String> splitList(String text) {
        List<String> items = new ArrayList<>();
        String[] parts = text.split(",");
        for (int i = 0; i < parts.length; i++) {
            String item = parts[i].trim();
            if (!item.isEmpty()) {
                items.add(item);
            }
        }
        return items;
    }

    /** Lê um inteiro; se vier vazio ou inválido, devolve 0. */
    private static int readInt(String text) {
        try {
            return Integer.parseInt(ask(text));
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    private static void showResponse(Response response) {
        if (response.getStatus() != Status.OK) {
            Style.showError(response.getStatus() + ": " + response.getErrorMessage());
            return;
        }

        if (response.hasMovie()) {
            showMovie(response.getMovie());
        } else if (response.hasMovies()) {
            MovieList list = response.getMovies();
            Style.showInfo("Quantidade: " + list.getMoviesCount());
            for (int i = 0; i < list.getMoviesCount(); i++) {
                Movie movie = list.getMovies(i);
                Style.printLine(" - " + movie.getTitle() + " (" + movie.getYear() + ") id=" + movie.getId());
            }
        } else {
            Style.showSuccess("Operação realizada com sucesso!");
        }
    }

    private static void showMovie(Movie movie) {
        Style.printLine(Style.SEPARATOR);
        Style.printLine(Style.BOLD + movie.getTitle() + Style.RESET + " (" + movie.getYear() + ")");
        Style.printLine("Id: " + movie.getId());
        Style.printLine("Categorias: " + String.join(", ", movie.getGenresList()));
        Style.printLine("Elenco: " + String.join(", ", movie.getCastList()));
        Style.printLine("Diretores: " + String.join(", ", movie.getDirectorsList()));
        Style.printLine("Duração: " + movie.getRuntime() + " min");
        Style.printLine("Sinopse: " + movie.getPlot());
        Style.printLine(Style.SEPARATOR);
    }
}