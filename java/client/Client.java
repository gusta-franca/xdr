/*
    Última atualização: 28/10/2026
    Estudante: Maria Eduarda Bambini
    Cliente do gerenciador de filmes (Mflix) via TCP com Protocol Buffers.
*/

package client;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.util.ArrayList;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.Scanner;

import common.Config;
import common.Protocol;
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

    /** Conecta ao servidor e repete o ciclo menu, pedido e resposta até o usuário sair. */
    public static void main(String[] args) {
        Config config = new Config(".env");

        try (Socket socket = new Socket(config.getServerHost(), config.getServerPort());
             InputStream in = socket.getInputStream();
             OutputStream out = socket.getOutputStream()) {

            Protocol protocol = new Protocol(in, out);
            Style.showSuccess("Conectado ao servidor " + config.getServerHost() + ":" + config.getServerPort());

            while (true) {
                showMenu();
                String option = ask("Escolha uma opção: ");

                if (option.equals("0")) {
                    Style.showInfo("Encerrando o cliente. Até logo!");
                    break;
                }

                Request request = buildRequest(option);
                if (request == null) {
                    continue;
                }

                protocol.writeMessage(request);
                Response response = protocol.readMessage(Response.parser());
                if (response == null) {
                    Style.showError("O servidor foi desconectado.");
                    break;
                }
                showResponse(response, option);
            }
        } catch (IOException e) {
            Style.showError(e.getMessage());
        } catch (NoSuchElementException e) {
            Style.showInfo("Entrada encerrada.");
        }
    }



    private static void showMenu() {
        String rule = Style.BOLD + Style.BLUE + "============================" + Style.RESET;

        Style.printLine("");
        Style.printLine(Style.BOLD + Style.BLUE + "======= MFlix Client =======" + Style.RESET);
        menuItem("1", "Criar filme");
        menuItem("2", "Consultar filme por ID");
        menuItem("3", "Atualizar filme");
        menuItem("4", "Excluir filme");
        menuItem("5", "Listar filmes por ator");
        menuItem("6", "Listar filmes por gênero");
        menuItem("0", "Sair");
        Style.printLine(rule);
    }

    private static void menuItem(String number, String label) {
        Style.printLine(Style.MAGENTA + Style.BOLD + number + "." + Style.RESET + " " + label);
    }

    /** Mostra o prompt e lê uma linha do teclado. */
    private static String ask(String label) {
        Style.currentPrompt = Style.prompt(label);
        System.out.print(Style.currentPrompt);
        System.out.flush();
        String line = reader.nextLine().trim();
        Style.currentPrompt = "";
        return line;
    }

    /** Monta o Request da opção escolhida; devolve null se a opção for inválida. */
    private static Request buildRequest(String option) {
        Request.Builder builder = Request.newBuilder();

        switch (option) {
            case "1":
                Style.showInfo("Opção selecionada: criar filme.");
                builder.setCreate(CreateMovieRequest.newBuilder().setMovie(readMovie("")));
                break;
            case "2":
                Style.showInfo("Opção selecionada: consultar filme por ID.");
                builder.setGet(GetMovieRequest.newBuilder().setId(ask("Digite o ID do filme: ")));
                break;
            case "3":
                Style.showInfo("Opção selecionada: atualizar filme.");
                String id = ask("Digite o ID do filme: ");
                builder.setUpdate(UpdateMovieRequest.newBuilder().setMovie(readMovie(id)));
                break;
            case "4":
                Style.showInfo("Opção selecionada: excluir filme.");
                builder.setDelete(DeleteMovieRequest.newBuilder().setId(ask("Digite o ID do filme: ")));
                break;
            case "5":
                Style.showInfo("Opção selecionada: pesquisar por ator.");
                builder.setListByActor(ListByActorRequest.newBuilder().setActor(ask("Digite o nome do ator: ")));
                break;
            case "6":
                Style.showInfo("Opção selecionada: pesquisar por gênero.");
                builder.setListByGenre(ListByGenreRequest.newBuilder().setGenre(ask("Digite o gênero dos filmes: ")));
                break;
            default:
                Style.showWarning("Opção inválida. Escolha um número de 0 a 6.");
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
        for (String part : text.split(",")) {
            String item = part.trim();
            if (!item.isEmpty()) {
                items.add(item);
            }
        }
        return items;
    }

    /** Lê um inteiro; se vier vazio ou inválido, devolve 0. */
    private static int readInt(String label) {
        try {
            return Integer.parseInt(ask(label));
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    private static void showResponse(Response response, String option) {
        if (response.getStatus() != Status.OK) {
            Style.showError(response.getStatus() + ": " + response.getErrorMessage());
            return;
        }

        if (response.hasMovie()) {
            if (option.equals("1")) {
                Style.showSuccess("Filme criado com sucesso.");
            } else if (option.equals("3")) {
                Style.showSuccess("Filme atualizado com sucesso.");
            }
            Style.printLine("");
            showMovie(response.getMovie());
        } else if (response.hasMovies()) {
            showMovieList(response.getMovies());
        } else {
            Style.showSuccess("Filme excluído com sucesso.");
        }
    }

    private static void showMovieList(MovieList list) {
        int total = list.getMoviesCount();
        if (total == 0) {
            Style.showWarning("Nenhum filme encontrado.");
            return;
        }

        Style.showSuccess(total + " filme(s) encontrado(s).");
        for (int i = 0; i < total; i++) {
            Style.printLine("");
            Style.printLine(Style.GRAY + "Filme " + (i + 1) + " de " + total + Style.RESET);
            showMovie(list.getMovies(i));
        }
    }

    private static void showMovie(Movie movie) {
        Style.field("ID", movie.getId());
        Style.field("Título", movie.getTitle() + " (" + movie.getYear() + ")");
        Style.field("Gêneros", String.join(", ", movie.getGenresList()));
        Style.field("Elenco", String.join(", ", movie.getCastList()));
        Style.field("Diretores", String.join(", ", movie.getDirectorsList()));
        Style.field("Duração", movie.getRuntime() + " min");
        Style.field("Classificação", movie.getRated());
        Style.field("Sinopse", movie.getPlot());
    }
}