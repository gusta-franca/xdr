/*
    Última atualização: 27/10/2026
    Estudante: Maria Eduarda Bambini
    Servidor do gerenciador de filmes (Mflix) via TCP com Protocol Buffers.
*/

package server;

import common.Config;
import common.Style;
import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;

public class Server {

    public static void main(String[] args) {
        Config config = new Config(".env");
        MovieRepository repository = new MovieRepository(config.getMongoUri());

        try (ServerSocket serverSocket = new ServerSocket(config.getServerPort())) {
            Style.showInfo("Servidor aguardando conexões na porta " + config.getServerPort());
            while (true) {
                Socket socket = serverSocket.accept();
                Style.showInfo("Cliente conectado: " + socket.getRemoteSocketAddress());
                new Thread(new Session(socket, repository)).start();
            }
        } catch (IOException e) {
            Style.showError(e.getMessage());
        }
    }
}