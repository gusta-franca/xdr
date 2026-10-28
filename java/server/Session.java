/*
   Data última atualização: 28/10/2026
   Estudante: Maria Eduarda Bambini
   Atende um cliente: lê cada Request, executa e devolve um Response.
*/

package server;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.util.List;

import common.Protocol;
import common.Style;
import mflix.Movie;
import mflix.MovieList;
import mflix.Request;
import mflix.Response;
import mflix.Status;

public class Session implements Runnable {

    private final Socket socket;
    private final MovieRepository repository;

    public Session(Socket socket, MovieRepository repository) {
        this.socket = socket;
        this.repository = repository;
    }

    @Override
    public void run() {
        try (Socket client = socket;
             InputStream in = client.getInputStream();
             OutputStream out = client.getOutputStream()) {

            Protocol protocol = new Protocol(in, out);

            while (true) {
                Request request = protocol.readMessage(Request.parser());
                if (request == null) {
                    break;
                }
                Style.showInfo("[" + client.getRemoteSocketAddress() + "] " + request.getCommandCase());

                Response response = handle(request);
                protocol.writeMessage(response);
            }
        } catch (IOException e) {
            Style.showWarning("Conexão encerrada: " + e.getMessage());
        }
        Style.showInfo("Cliente desconectado");
    }

    /** Escolhe o método de acordo com o comando recebido e trata os erros. */
    private Response handle(Request request) {
        try {
            switch (request.getCommandCase()) {
                case CREATE:
                    return create(request.getCreate().getMovie());
                case GET:
                    return get(request.getGet().getId());
                case UPDATE:
                    return update(request.getUpdate().getMovie());
                case DELETE:
                    return delete(request.getDelete().getId());
                case LIST_BY_ACTOR:
                    return listByActor(request.getListByActor().getActor());
                case LIST_BY_GENRE:
                    return listByGenre(request.getListByGenre().getGenre());
                default:
                    return error(Status.INVALID_ARGUMENT, "Comando não informado");
            }
        } catch (IllegalArgumentException e) {
            return error(Status.INVALID_ARGUMENT, "Id inválido");
        } catch (Exception e) {
            e.printStackTrace();
            Style.showError(e.toString());
            return error(Status.INTERNAL_ERROR, "Erro interno no servidor");
        }
    }

    /* Métodos/operações disponíveis. */
    
    private Response create(Movie movie) {
        if (movie.getTitle().isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "O título não pode ser vazio");
        }
        String newId = repository.insert(movie);
        Movie saved = movie.toBuilder().setId(newId).build();
        return Response.newBuilder().setStatus(Status.OK).setMovie(saved).build();
    }

    private Response get(String id) {
        if (id.isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "O id não pode ser vazio");
        }
        Movie movie = repository.findById(id);
        if (movie == null) {
            return error(Status.NOT_FOUND, "Filme não encontrado");
        }
        return Response.newBuilder().setStatus(Status.OK).setMovie(movie).build();
    }

    private Response update(Movie movie) {
        if (movie.getId().isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "O id não pode ser vazio");
        }
        boolean found = repository.update(movie);
        if (!found) {
            return error(Status.NOT_FOUND, "Filme não encontrado");
        }
        Movie updated = repository.findById(movie.getId());
        return Response.newBuilder().setStatus(Status.OK).setMovie(updated).build();
    }

    private Response delete(String id) {
        if (id.isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "O id não pode ser vazio");
        }
        boolean found = repository.delete(id);
        if (!found) {
            return error(Status.NOT_FOUND, "Filme não encontrado");
        }
        return Response.newBuilder().setStatus(Status.OK).build();
    }

    private Response listByActor(String actor) {
        if (actor.isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "O nome do ator não pode ser vazio");
        }
        return listResponse(repository.findByActor(actor));
    }

    private Response listByGenre(String genre) {
        if (genre.isEmpty()) {
            return error(Status.INVALID_ARGUMENT, "A categoria não pode ser vazia");
        }
        return listResponse(repository.findByGenre(genre));
    }

    private Response listResponse(List<Movie> movies) {
        MovieList list = MovieList.newBuilder().addAllMovies(movies).build();
        return Response.newBuilder().setStatus(Status.OK).setMovies(list).build();
    }

    private Response error(Status status, String message) {
        return Response.newBuilder().setStatus(status).setErrorMessage(message).build();
    }
}