/*
   Data de criação: 26/10/2026
   Estudante: Gustavo Martins França
   Definição de uma sessão entre o servidor e um cliente
*/

package main

import (
	"context"
	"errors"
	"io"
	"log"
	"net"
	"time"

	"mflix/internal/pb"
	"mflix/internal/protocol"
	"mflix/internal/repository"
)

type Session struct {
	conn  net.Conn
	proto *protocol.Protocol
	repo  *repository.MovieRepository
}

// Session constructor
func NewSession(conn net.Conn, repo *repository.MovieRepository) *Session {
	return &Session{
		conn:  conn,
		proto: protocol.NewProtocol(conn),
		repo:  repo,
	}
}

// Reads requests until the client disconnects
func (s *Session) handleConnection() {
	defer s.conn.Close()
	log.Printf("Client connected: %s", s.conn.RemoteAddr())

	for {
		var req pb.Request

		if err := s.proto.ReadMessage(&req); err != nil {
			if err != io.EOF {
				log.Printf("Read error from %s: %s", s.conn.RemoteAddr(), err)
			}

			log.Printf("Connection finished: %s", s.conn.RemoteAddr())
			return
		}

		ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		s.HandleCommand(ctx, &req)
		cancel()
	}
}

// Dispatches the request to the appropriate handler
func (s *Session) HandleCommand(ctx context.Context, req *pb.Request) {
	switch cmd := req.Command.(type) {
	case *pb.Request_Create:
		s.Create(ctx, cmd.Create.GetMovie())
		s.serverLog("CREATE")

	case *pb.Request_Get:
		s.Get(ctx, cmd.Get.GetId())
		s.serverLog("GET")

	case *pb.Request_Update:
		s.Update(ctx, cmd.Update.GetMovie())
		s.serverLog("UPDATE")

	case *pb.Request_Delete:
		s.Delete(ctx, cmd.Delete.GetId())
		s.serverLog("DELETE")

	case *pb.Request_ListByActor:
		s.ListByActor(ctx, cmd.ListByActor.GetActor())
		s.serverLog("LIST_BY_ACTOR")

	case *pb.Request_ListByGenre:
		s.ListByGenre(ctx, cmd.ListByGenre.GetGenre())
		s.serverLog("LIST_BY_GENRE")

	default:
		s.sendError(pb.Status_INVALID_ARGUMENT, "no command set")
		s.serverLog("UNKNOWN")
	}
}

// Handles CREATE
func (s *Session) Create(ctx context.Context, m *pb.Movie) {
	if m == nil || m.Title == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "title is required")
		return
	}

	created, err := s.repo.Create(ctx, m)

	if err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.sendMovie(created)
}

// Handles GET
func (s *Session) Get(ctx context.Context, id string) {
	if id == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "id is empty")
		return
	}

	movie, err := s.repo.Get(ctx, id)

	if err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.sendMovie(movie)
}

// Handles UPDATE
func (s *Session) Update(ctx context.Context, m *pb.Movie) {
	if m == nil || m.Id == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "id is empty")
		return
	}

	if m.Title == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "title is required")
		return
	}

	updated, err := s.repo.Update(ctx, m)

	if err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.sendMovie(updated)
}

// Handles DELETE
func (s *Session) Delete(ctx context.Context, id string) {
	if id == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "id is empty")
		return
	}

	if err := s.repo.Delete(ctx, id); err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.send(&pb.Response{Status: pb.Status_OK})
}

// Handles LIST_BY_ACTOR
func (s *Session) ListByActor(ctx context.Context, actor string) {
	if actor == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "actor is empty")
		return
	}

	movies, err := s.repo.ListByActor(ctx, actor)

	if err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.sendMovies(movies)
}

// Handles LIST_BY_GENRE
func (s *Session) ListByGenre(ctx context.Context, genre string) {
	if genre == "" {
		s.sendError(pb.Status_INVALID_ARGUMENT, "genre is empty")
		return
	}

	movies, err := s.repo.ListByGenre(ctx, genre)

	if err != nil {
		s.sendRepositoryError(err)
		return
	}

	s.sendMovies(movies)
}

// Writes a response to the client
func (s *Session) send(resp *pb.Response) {
	if err := s.proto.WriteMessage(resp); err != nil {
		log.Printf("Error sending response: %s", err)
	}
}

// Helper for sending a single movie
func (s *Session) sendMovie(m *pb.Movie) {
	s.send(&pb.Response{
		Status:  pb.Status_OK,
		Payload: &pb.Response_Movie{Movie: m},
	})
}

// Helper for sending movies
func (s *Session) sendMovies(ms []*pb.Movie) {
	s.send(&pb.Response{
		Status:  pb.Status_OK,
		Payload: &pb.Response_Movies{Movies: &pb.MovieList{Movies: ms}},
	})
}

// Helper for sending errors
func (s *Session) sendError(status pb.Status, msg string) {
	s.send(&pb.Response{
		Status:       status,
		ErrorMessage: msg,
	})
}

// Converts a repo error into a status for the client
func (s *Session) sendRepositoryError(err error) {
	switch {
	case errors.Is(err, repository.ErrNotFound):
		s.sendError(pb.Status_NOT_FOUND, err.Error())

	case errors.Is(err, repository.ErrInvalidID):
		s.sendError(pb.Status_INVALID_ARGUMENT, err.Error())

	default:
		log.Printf("Internal error: %s", err)
		s.sendError(pb.Status_INTERNAL_ERROR, "internal error")
	}
}

// Logs every executed command
func (s *Session) serverLog(command string) {
	log.Printf("client %s executed %s\n", s.conn.RemoteAddr(), command)
}
