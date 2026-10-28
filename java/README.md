# Atividade RED – Gerenciamento de Filmes

Serviço de gerenciamento de filmes sobre a coleção `movies` do banco `sample_mflix` (MongoDB), acessado remotamente via TCP com mensagens em Protocol Buffers.

* Última atualização: 28/10/2026
* Estudante: Maria Eduarda Bambini 

## Requisitos

- JDK (Java 17 ou superior)
- `protoc` (Protocol Buffers)
- Um MongoDB com o dataset `sample_mflix`, seja com Docker (local) ou MongoDB Atlas.

## Funcionalidades

- CRUD de filmes (os campos `imdb`, `tomatoes` e `awards` são ignorados).
- Listagem de filmes por ator.
- Listagem de filmes por categoria.
- Tratamento de erros (id inexistente, id inválido, campos vazios) são devolvidos ao cliente com um status e uma mensagem.

## Configuração

Crie uma cópia do arquivo de variáveis de ambiente na raíz do projeto, e o altere se necessário:

``` cp .env.example .env ```

## Como executar

Na primeira execução, baixe os jars do driver do MongoDB na raíz do repositório:

```bash
make mongo-libs
```

Em um terminal, suba o servidor:

```bash
make run-server
```

Em outro terminal, abra o cliente:

```bash
make run-client
```

## Uso do cliente

O cliente mostra um menu numérico, e os dados de cada opção são pedidos em seguida:

| Opção | Ação |
|---|---|
| `1` | Criar filme (pede os campos um por um). |
| `2` | Consultar filme por ID. |
| `3` | Atualizar filme (pede o ID e depois os campos). |
| `4` | Excluir filme. |
| `5` | Listar filmes por ator. |
| `6` | Listar filmes por gênero. |
| `0` | Sair. |

## Protocolo

### Mensagens

As mensagens são as de `proto/mflix.proto`:

- **Request**: um dos comandos `create`, `get`, `update`, `delete`, `list_by_actor` ou `list_by_genre`.
- **Response**: um `Status`, uma mensagem de erro (quando o status não é `OK`) e, opcionalmente, um filme (`movie`) ou uma lista de filmes (`movies`).

A comunicação é de pedido e resposta: o cliente envia um `Request` e espera o `Response` antes de enviar o próximo pedido. O servidor cria uma thread por cliente conectado (classe `Session`), e cada cliente pode enviar vários pedidos na mesma conexão.

### Enquadramento no TCP

O TCP é um fluxo de bytes, então cada mensagem (pedido ou resposta) é enviada com um cabeçalho (4 bytes) que informa o tamanho do corpo.

- O cabeçalho e o corpo são enviados em uma única escrita.
- Quem lê recebe os 4 bytes do cabeçalho e depois exatamente a quantidade de bytes informada.
- O tamanho máximo do corpo é de 16 MiB. Tamanhos negativos ou acima do limite são rejeitados, e a conexão é encerrada.
- Se a conexão for fechada antes do início de uma nova mensagem, a leitura devolve "fim da conexão" (`null` no Java), e não um erro.
- Em Java, o formato está em `java/common/Protocol.java` (`readMessage` e `writeMessage`).

### Operações no servidor

| Operação | Validação | Resposta |
|---|---|---|
| `create` | título obrigatório | `OK` com o filme e o id gerado pelo banco |
| `get` | id obrigatório | `OK` com o filme, ou `NOT_FOUND` |
| `update` | id obrigatório | `OK` com o filme atualizado, ou `NOT_FOUND` |
| `delete` | id obrigatório | `OK` sem corpo, ou `NOT_FOUND` |
| `list_by_actor` | ator obrigatório | `OK` com a lista (pode ser vazia) |
| `list_by_genre` | categoria obrigatória | `OK` com a lista (pode ser vazia) |

### Status

| Status | Quando ocorre |
|---|---|
| `OK` | Operação realizada. |
| `NOT_FOUND` | O id informado não existe (`get`, `update`, `delete`). |
| `INVALID_ARGUMENT` | Campo obrigatório vazio, id mal formado ("Id inválido") ou pedido sem comando ("Comando não informado"). |
| `INTERNAL_ERROR` | Falha inesperada, como banco indisponível. O cliente recebe "Erro interno no servidor", e o detalhe aparece no console do servidor. |

## Decisões de projeto

- O campo `released` é salvo como texto, e a data de `lastupdated` é gravada como texto no momento da criação e da atualização.
- Campos ausentes ou com tipo diferente no dataset (por exemplo, `runtime` ou `year`) são lidos como vazio ou `0`.
- O proto3 não diferencia campo vazio de campo não informado, por isso a atualização não permite limpar um campo.