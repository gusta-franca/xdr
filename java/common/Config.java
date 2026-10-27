/*
    Última atualização: 27/10/2026
    Aluna: Maria Eduarda Bambini
    Arquivo compartilhado (cliente e servidor) responsável por ler as configurações do arquivo .env.
*/

package common;
 
import java.io.FileReader;
import java.io.IOException;
import java.util.Properties;

/**
 * Config: lê as configurações (SERVER_ADDR e BASE_DIR) do arquivo .env.
 */
public class Config {

    private final Properties properties = new Properties();
    
    public Config(String fileName) {
        try (FileReader reader = new FileReader(fileName)) {
            properties.load(reader);
        } catch (IOException e) {
            System.out.println("Arquivo " + fileName + " não encontado. Usando valores padrão.");
        }
    }

    public String get(String key, String defaultValue) {
        String fromEnvironment = System.getenv(key);
        if (fromEnvironment != null) {
            return fromEnvironment;
        }

        String value = properties.getProperty(key, defaultValue);
        return value.trim();
    }

    private String[] splitAddress() {
        String address = get("SERVER_ADDR", "127.0.0.1:5000");
        String[] parts = address.split(":");

        if (parts.length != 2) {
            throw new IllegalArgumentException(
                "SERVER_ADDR inválido. Use o formato host:porta (ex.: 127.0.0.1:5000)");
        }

        return parts;
    }

    public String getServerHost() {
        String[] parts = splitAddress();
        return parts[0];
    }

    public int getServerPort() {
        String[] parts = splitAddress();
        return Integer.parseInt(parts[1]);
    }

}
