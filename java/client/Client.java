package client;

import java.util.Scanner;

import common.Config;

public class Client {
    
    Config config = new Config(".env");
    String serverHost = config.getServerHost();
    int serverPort = config.getServerPort();

    public Client(String serverHost, int serverPort) {
        this.serverHost = serverHost;
        this.serverPort = serverPort;
    }

    public static void main(String[] args) {
        try (Scanner scanner = new Scanner(System.in);
            Socket socket = new Socket(serverHost, serverPort);
            Connection connection = new Connection(socket)
        ) {
            while (true) { 
                
            }
        }
    }

}
