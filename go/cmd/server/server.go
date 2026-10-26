/*
   Data de criação: 24/10/2026
   Estudante: Gustavo Martins França
   Definição do servidor
*/

package main

import (
	"log"
	"mflix/internal/repository"
	"net"
	"os"
)

type Server struct {
	addr     string
	repo     *repository.MovieRepository
	listener net.Listener
}

func NewServer(addr string, repo *repository.MovieRepository) *Server {
	return &Server{
		addr: addr,
		repo: repo,
	}
}

// Listens to addr and creates goroutines to handle each session
func (s *Server) Run() {
	var err error
	s.listener, err = net.Listen("tcp", s.addr)

	if err != nil {
		log.Fatalf("Error listening to %s", s.addr)
	}

	defer s.listener.Close()

	if err := os.MkdirAll("logs/", os.ModePerm); err != nil {
		log.Fatalf("Could not create log directory: %s", err)
	}

	logFile, err := os.OpenFile("logs/server.log", os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0666)

	if err != nil {
		log.Fatalf("Could not open log file: %s", err)
	}

	defer logFile.Close()

	log.SetOutput(logFile)

	for {
		conn, err := s.listener.Accept()

		if err != nil {
			log.Println("Error accepting connection")
			continue
		}

		session := NewSession(conn, s.repo)
		go session.handleConnection()
	}
}
