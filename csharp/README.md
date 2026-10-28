# MFlix — C#

Implementação em C# do projeto MFlix, um serviço de gerenciamento de filmes que utiliza MongoDB para armazenamento, TCP para comunicação entre cliente e servidor e Protocol Buffers para a representação e serialização das mensagens.

## Estrutura do projeto

```text
csharp/
├── Mflix.Protocol/
├── Mflix.Server/
├── Mflix.Client/
└── Mflix.slnx
```

- **Mflix.Protocol:** contém a configuração para gerar os tipos C# a partir do arquivo `proto/mflix.proto`.
- **Mflix.Server:** aplicação responsável por receber requisições TCP, executar as operações solicitadas e retornar respostas.
- **Mflix.Client:** aplicação cliente que envia requisições ao servidor e interpreta as respostas.
- **Mflix.slnx:** solução que reúne os projetos C#.

## Tecnologias

- C#
- .NET
- MongoDB
- TCP
- Protocol Buffers (Protobuf)

## Protocolo de comunicação

As mensagens de comunicação são definidas em `proto/mflix.proto`, compartilhado entre as implementações do projeto.

O Protocol Buffers permite serializar e desserializar as mensagens em formato binário. Isso possibilita que aplicações desenvolvidas em linguagens diferentes, como C# e Go, utilizem o mesmo formato de comunicação.

## Pré-requisitos

- .NET SDK compatível com a versão definida nos arquivos `.csproj`.
- MongoDB em execução e acessível pelo servidor.

## Compilação

Na raiz do repositório, execute:

```bash
dotnet build csharp/Mflix.slnx
```

A compilação do projeto `Mflix.Protocol` gera os tipos C# necessários a partir do arquivo `.proto`.

## Configuração

As configurações de ambiente necessárias para a conexão com o MongoDB e a execução do servidor serão documentadas conforme a implementação.

Consulte o arquivo `.env.example` na raiz do repositório para verificar as variáveis de ambiente previstas para o projeto.

## Funcionalidades previstas

- Criar, consultar, atualizar e excluir filmes.
- Listar filmes por ator e gênero.
- Validar identificadores e campos obrigatórios.
- Retornar respostas com códigos de status e mensagens de erro.
- Permitir comunicação com clientes implementados em outras linguagens.

