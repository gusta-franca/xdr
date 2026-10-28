## Sobre o programa

Estudante: Gustavo Martins França

Aplicação cliente-servidor desenvolvida em Go para a manipulação de registros de filmes em um banco de dados MongoDB, utilizando ProtocolBuf para o processo de marshalling/unmarshalling.

Os comandos executados em uma sessão serão registrados em `logs/server.log`.

## Dependências

- Go
- Makefile

## Configuração

Crie uma cópia de `.env.example`, a renomeie para `.env`, mova-a para `go/` e altere as variáveis conforme necessário.

## Como executar

Compile o arquivo `proto/mflix.proto` para a linguagem Go (no diretório raiz):

```bash
make proto-go
```

Compile o servidor e o cliente:

```bash
cd go
make build
```

Inicie o servidor (terminal 1):

```bash
make run-server
```

Inicie o cliente (terminal 2):

```bash
make run-client
```
