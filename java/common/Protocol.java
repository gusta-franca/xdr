/*
    Última atualização: 28/10/2026
    Estudante: Maria Eduarda Bambini
    Protocolo de enquadramento de mensagens sobre TCP: cabeçalho de 4 bytes (big-endian) com o tamanho do corpo, seguido do corpo serializado em protobuf.
*/

package common;

import java.io.DataInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.ByteBuffer;

import com.google.protobuf.Message;
import com.google.protobuf.Parser;

public class Protocol {

    private static final int HEADER_SIZE = 4;
    private static final int MAX_MESSAGE_SIZE = 16 * 1024 * 1024;

    private final DataInputStream in;
    private final OutputStream out;

    public Protocol(InputStream in, OutputStream out) {
        this.in = new DataInputStream(in);
        this.out = out;
    }

    /**
     * Lê o cabeçalho de 4 bytes (big-endian) e depois exatamente o número de bytes informado.
     * Devolve null se a conexão foi encerrada antes do início de uma nova mensagem.
     */
    public <T extends Message> T readMessage(Parser<T> parser) throws IOException {
        int first = in.read();
        if (first == -1) {
            return null;
        }

        byte[] header = new byte[HEADER_SIZE];
        header[0] = (byte) first;
        in.readFully(header, 1, HEADER_SIZE - 1);

        int length = ByteBuffer.wrap(header).getInt();
        if (length < 0 || length > MAX_MESSAGE_SIZE) {
            throw new IOException("Tamanho de mensagem inválido: " + length);
        }

        byte[] data = new byte[length];
        in.readFully(data);
        return parser.parseFrom(data);
    }

    /** Escreve o cabeçalho de 4 bytes (big-endian) seguido do corpo, em uma única escrita. */
    public void writeMessage(Message message) throws IOException {
        byte[] body = message.toByteArray();

        ByteBuffer buffer = ByteBuffer.allocate(HEADER_SIZE + body.length);
        buffer.putInt(body.length);
        buffer.put(body);

        out.write(buffer.array());
        out.flush();
    }
}