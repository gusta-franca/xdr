# MFlix — C#

Aluna: Caroline Marques Lau

Implementação em C# de um serviço de gerenciamento de filmes que utiliza MongoDB para armazenamento, TCP para comunicação e Protocol Buffers (Protobuf) para serialização das mensagens.

## Tecnologias

- C#
- .NET
- MongoDB
- TCP
- Protocol Buffers (Protobuf)

## Pré-requisitos

- .NET SDK compatível com os projetos.
- MongoDB em execução e acessível pelo servidor.

## Configuração

Consulte o arquivo `.env.example` na raiz do repositório para verificar as variáveis de ambiente necessárias.

## Compilação

Execute na raiz do repositório:

```bash
dotnet build Mflix.slnx
```

## Execução

Inicie o servidor:

```bash
dotnet run --project csharp/server/Server.csproj
```

Em outro terminal, execute o cliente:

```bash
dotnet run --project csharp/client/Client.csproj
```

## Funcionalidades

- Criar, consultar, atualizar e excluir filmes.
- Listar filmes por ator e gênero.

## Comunicação

Cada operação tem sua própria conexão, evitando que uma requisição dependa do estado de uma conexão anterior.