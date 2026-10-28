/*
    Última atualização: 28/10/2026
    Estudante: Maria Eduarda Bambini
    Arquivo compartilhado (cliente e servidor) responsável por ler as configurações do arquivo .env.
*/

package common;
 
import java.io.FileReader;
import java.io.IOException;
import java.util.Properties;

/**
 * Lê as configurações do arquivo .env.
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

    /** Devolve a URI de conexão com o MongoDB. */
    public String getMongoUri() {
        return get("MONGODB_URI", "mongodb://localhost:27017");
    }

    /** Devolve o nome do banco de dados. */
    public String getMongoDb() {
        return get("MONGODB_DB", "sample_mflix");
    }

    /** Devolve o nome da coleção de filmes. */
    public String getMongoCollection() {
        return get("MONGODB_COLLECTION", "movies");
    }
}

