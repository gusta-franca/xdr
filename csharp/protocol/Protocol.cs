﻿/*
    Data de criação: 28/10/2026
    Aluna: Caroline Marques Lau
    Arquivo do protocolo utilizado para comunicação
*/

using System.Buffers.Binary;
using Google.Protobuf;

namespace Protocol;

public static class MessageProtocol
// Não é necessário uma instância para utilizar os métodos (static)
{
    public static async Task WriteMessage(Stream stream, IMessage message)
    {
        byte[] data = message.ToByteArray();
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(
            header, (uint)data.Length);
        await stream.WriteAsync(header);
        await stream.WriteAsync(data);
    }

    public static async Task<T> ReadMessage<T>(
        Stream stream,
        MessageParser<T> parser // Interpreta os bytes e reconstroi uma mensagem 
        )
        where T : IMessage<T> // Restringe T aos tipos de mensagens compatíveis
    {
        byte[] header = new byte[4];

        await stream.ReadExactlyAsync(header);

        uint length =
            BinaryPrimitives.ReadUInt32BigEndian(header);

        if (length > int.MaxValue)
            throw new InvalidDataException("Mensagem muito grande.");

        byte[] data = new byte[(int)length];

        await stream.ReadExactlyAsync(data);

        return parser.ParseFrom(data);
    }
}
