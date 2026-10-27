/*
   Data de criação: 27/10/2026
   Estudante: Gustavo Martins França
   Definição do cliente
*/

package main

import (
	"bufio"
	"fmt"
	"log"
	"net"
	"os"
	"strconv"
	"strings"

	"mflix/internal/pb"
	"mflix/internal/protocol"
)

type Client struct {
	addr        string
	conn        net.Conn
	proto       *protocol.Protocol
	inputReader *bufio.Reader
}

// Client constructor
func NewClient(addr string) *Client {
	return &Client{
		addr: addr,
	}
}

// Connects a client to a server
func (c *Client) Connect() error {
	var err error
	c.conn, err = net.Dial("tcp", c.addr)

	if err != nil {
		return err
	}

	c.inputReader = bufio.NewReader(os.Stdin)
	c.proto = protocol.NewProtocol(c.conn)

	fmt.Printf("Connected on %s\n", c.addr)
	return nil
}

// Shows the menu and dispatches the chosen option until the user exits
func (c *Client) Run() {
	err := c.Connect()

	if err != nil {
		log.Fatal("Error connecting to server")
	}

	defer c.conn.Close()

	for {
		c.printMenu()

		var err error

		switch c.prompt("\n>>") {
		case "1":
			err = c.Create()

		case "2":
			err = c.Delete()

		case "3":
			err = c.Update()

		case "4":
			err = c.Get()

		case "5":
			err = c.ListByActor()

		case "6":
			err = c.ListByGenre()

		case "0":
			fmt.Printf("Disconnected from %s\n", c.addr)
			return

		default:
			fmt.Println("Invalid option")
		}

		if err != nil {
			log.Printf("Connection error: %s", err)
			return
		}
	}
}

// Prints every available option to the client
func (c *Client) printMenu() {
	fmt.Println("1. Adicionar novo filme")
	fmt.Println("2. Remover filme")
	fmt.Println("3. Alterar filme")
	fmt.Println("4. Buscar filme por ID")
	fmt.Println("5. Listar filmes por ator")
	fmt.Println("6. Listar filmes por categoria")
	fmt.Println("0. Sair")
}

// Handles option 1
func (c *Client) Create() error {
	m := &pb.Movie{
		Title:     c.prompt("Título"),
		Plot:      c.prompt("Sinopse"),
		Fullplot:  c.prompt("Sinopse completa"),
		Genres:    splitList(c.prompt("Gêneros (separados por vírgula)")),
		Cast:      splitList(c.prompt("Elenco (separados por vírgula)")),
		Directors: splitList(c.prompt("Diretores (separados por vírgula)")),
		Countries: splitList(c.prompt("Países (separados por vírgula)")),
		Year:      toInt32(c.prompt("Ano")),
		Runtime:   toInt32(c.prompt("Duração (min)")),
		Rated:     c.prompt("Classificação"),
		Type:      "movie",
	}

	res, err := c.sendRequest(
		&pb.Request{
			Command: &pb.Request_Create{
				Create: &pb.CreateMovieRequest{
					Movie: m,
				},
			},
		},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	fmt.Println("Filme criado:")
	printMovie(res.GetMovie())
	return nil
}

// Handles option 2
func (c *Client) Delete() error {
	res, err := c.sendRequest(
		&pb.Request{
			Command: &pb.Request_Delete{
				Delete: &pb.DeleteMovieRequest{
					Id: c.prompt("ID"),
				},
			},
		},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	fmt.Println("Filme removido")
	return nil
}

// Handles option 3
func (c *Client) Update() error {
	id := c.prompt("ID do filme")

	res, err := c.sendRequest(
		&pb.Request{
			Command: &pb.Request_Get{
				Get: &pb.GetMovieRequest{
					Id: id,
				},
			},
		},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	m := res.GetMovie()
	fmt.Println("(Enter mantém o valor atual)")

	if s := c.prompt("Título [" + m.Title + "]"); s != "" {
		m.Title = s
	}

	if s := c.prompt("Sinopse [" + m.Plot + "]"); s != "" {
		m.Plot = s
	}

	if s := c.prompt("Gêneros [" + strings.Join(m.Genres, ",") + "]"); s != "" {
		m.Genres = splitList(s)
	}

	if s := c.prompt("Elenco [" + strings.Join(m.Cast, ",") + "]"); s != "" {
		m.Cast = splitList(s)
	}

	if s := c.prompt("Diretores [" + strings.Join(m.Directors, ",") + "]"); s != "" {
		m.Directors = splitList(s)
	}

	if s := c.prompt("Ano [" + strconv.Itoa(int(m.Year)) + "]"); s != "" {
		m.Year = toInt32(s)
	}

	res, err = c.sendRequest(&pb.Request{
		Command: &pb.Request_Update{Update: &pb.UpdateMovieRequest{Movie: m}},
	})

	if err != nil || !checkStatus(res) {
		return err
	}

	fmt.Println("Filme atualizado:")
	printMovie(res.GetMovie())
	return nil
}

// Handles option 4
func (c *Client) Get() error {
	res, err := c.sendRequest(
		&pb.Request{
			Command: &pb.Request_Get{
				Get: &pb.GetMovieRequest{
					Id: c.prompt("ID"),
				},
			},
		},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	printMovie(res.GetMovie())
	return nil
}

// Handles option 5
func (c *Client) ListByActor() error {
	res, err := c.sendRequest(&pb.Request{
		Command: &pb.Request_ListByActor{
			ListByActor: &pb.ListByActorRequest{
				Actor: c.prompt("Nome do ator"),
			},
		},
	},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	printMovies(res.GetMovies().GetMovies()) // protoc generated two functions with the same name
	return nil
}

// Handles option 6
func (c *Client) ListByGenre() error {
	res, err := c.sendRequest(
		&pb.Request{
			Command: &pb.Request_ListByGenre{
				ListByGenre: &pb.ListByGenreRequest{
					Genre: c.prompt("Categoria"),
				},
			},
		},
	)

	if err != nil || !checkStatus(res) {
		return err
	}

	printMovies(res.GetMovies().GetMovies())
	return nil
}

// Sends a request and waits for its resonse
func (c *Client) sendRequest(req *pb.Request) (*pb.Response, error) {
	if err := c.proto.WriteMessage(req); err != nil {
		return nil, fmt.Errorf("Error sending request: %w", err)
	}

	var res pb.Response

	if err := c.proto.ReadMessage(&res); err != nil {
		return nil, fmt.Errorf("Error eading response: %w", err)
	}

	return &res, nil
}

// Prompts the user for an option and returns the trimmed line
func (c *Client) prompt(in string) string {
	fmt.Printf("%s: ", in)

	str, err := c.inputReader.ReadString('\n')

	if err != nil {
		log.Fatalf("Error reading input: %s", err)
	}

	return strings.TrimSpace(str)
}
