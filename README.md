# Gerenciamento de filmes

Alunos: Caroline Marques Lau, Gustavo Martins França e Maria Eduarda Bambini

Disciplina: Sistemas Distribuídos

Implementar um serviço de gerenciamento de filmes usando os dados providos pela coleção movies da base de dados
[Mflix](https://www.mongodb.com/docs/atlas/sample-data/sample-mflix/) 

## Funcionalidades

- Criar, consultar, atualizar e excluir filmes.
- Listar filmes por ator e gênero.

## Banco de Dados

O SGBD utilizado é o MongoDB. Para iniciar um container com os dados de exemplo do MongoDB Atlas (`sample_mflix` e outras coleções), execute na raiz do repositório:

```bash
make setup
```

O banco ficará disponível em `mongodb://localhost:27017`.

## Protocolo de Comunicação

As mensagens são transmitidas por TCP. Cada pacote contém 4 bytes que indicam o tamanho do conteúdo, seguidos pelos dados da mensagem.

O arquivo `mflix.proto` define o contrato de comunicação entre cliente e servidor, especificando a estrutura das mensagens e as operações disponíveis.

- **`Movie`:** define os campos de um filme.
- **`Request`:** identifica a operação solicitada, como criar, consultar, atualizar, excluir ou listar filmes.
- **`Response`:** define a estrutura da resposta, incluindo o status, possíveis mensagens de erro e os dados retornados.
- **`Status`:** define os códigos de resultado, como sucesso, registro não encontrado e argumento inválido.

O Protocol Buffers (Protobuf) gera as classes utilizadas pelas diferentes linguagens do projeto e realiza a serialização e desserialização das mensagens em formato binário. Assim, clientes e servidores implementados em C#, Go, Java ou outras linguagens podem se comunicar seguindo o mesmo padrão.

Os números associados aos campos no arquivo `.proto` identificam esses campos no formato binário e devem ser preservados para manter a compatibilidade entre as versões do protocolo.