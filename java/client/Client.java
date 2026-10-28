/*
   Cliente do gerenciador de filmes (Mflix) via TCP com Protocol Buffers.
*/

package client;

import common.Config;
import common.Style;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.util.Scanner;
import mflix.*;

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
                String option = ask("> ").toUpperCase();

                if (option.equals("EXIT")) {
                    break;
                }

                Request request = buildRequest(option);
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
        Style.showInfo("Comandos: CREATE, GET, UPDATE, DELETE, ACTOR, GENRE, EXIT");
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

    private static Request buildRequest(String option) {
        Request.Builder builder = Request.newBuilder();

        if (option.equals("CREATE")) {
            Movie movie = readMovie("");
            builder.setCreate(CreateMovieRequest.newBuilder().setMovie(movie));
        } else if (option.equals("GET")) {
            String id = ask("Id: ");
            builder.setGet(GetMovieRequest.newBuilder().setId(id));
        } else if (option.equals("UPDATE")) {
            String id = ask("Id do filme: ");
            Movie movie = readMovie(id);
            builder.setUpdate(UpdateMovieRequest.newBuilder().setMovie(movie));
        } else if (option.equals("DELETE")) {
            String id = ask("Id: ");
            builder.setDelete(DeleteMovieRequest.newBuilder().setId(id));
        } else if (option.equals("ACTOR")) {
            String actor = ask("Ator: ");
            builder.setListByActor(ListByActorRequest.newBuilder().setActor(actor));
        } else if (option.equals("GENRE")) {
            String genre = ask("Categoria: ");
            builder.setListByGenre(ListByGenreRequest.newBuilder().setGenre(genre));
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

        String[] genres = ask("Categorias (separe por vírgula): ").split(",");
        String[] cast = ask("Elenco (separe por vírgula): ").split(",");
        String[] directors = ask("Diretores (separe por vírgula): ").split(",");
        String[] countries = ask("Países (separe por vírgula): ").split(",");

        for (int i = 0; i < genres.length; i++) {
            if (!genres[i].trim().isEmpty()) {
                movie.addGenres(genres[i].trim());
            }
        }
        for (int i = 0; i < cast.length; i++) {
            if (!cast[i].trim().isEmpty()) {
                movie.addCast(cast[i].trim());
            }
        }
        for (int i = 0; i < directors.length; i++) {
            if (!directors[i].trim().isEmpty()) {
                movie.addDirectors(directors[i].trim());
            }
        }
        for (int i = 0; i < countries.length; i++) {
            if (!countries[i].trim().isEmpty()) {
                movie.addCountries(countries[i].trim());
            }
        }
        return movie.build();
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