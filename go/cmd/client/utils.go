/*
   Data de criação: 27/10/2026
   Estudante: Gustavo Martins França
   Utilidades do cliente
*/

package main

import (
	"fmt"
	"mflix/internal/pb"
	"strconv"
	"strings"
)

// Returns true when the server status is OK; otherwise prints the error
func checkStatus(resp *pb.Response) bool {
	if resp.Status != pb.Status_OK {
		fmt.Printf("Erro [%s]: %s\n", resp.Status, resp.ErrorMessage)
		return false
	}

	return true
}

// Splits a comma separated list into it's tokens
func splitList(s string) []string {
	var tokens []string

	for t := range strings.SplitSeq(s, ",") {
		if t = strings.TrimSpace(t); t != "" {
			tokens = append(tokens, t)
		}
	}

	return tokens
}

// Converts a string to int32
func toInt32(s string) int32 {
	n, err := strconv.Atoi(s)

	if err != nil {
		return 0
	}

	return int32(n)
}

// Helper to print a movie
func printMovie(m *pb.Movie) {
	fmt.Println()
	fmt.Printf("ID:            %s\n", m.Id)
	fmt.Printf("Título:        %s (%d)\n", m.Title, m.Year)
	fmt.Printf("Gêneros:       %s\n", strings.Join(m.Genres, ", "))
	fmt.Printf("Elenco:        %s\n", strings.Join(m.Cast, ", "))
	fmt.Printf("Diretores:     %s\n", strings.Join(m.Directors, ", "))
	fmt.Printf("Duração:       %d min\n", m.Runtime)
	fmt.Printf("Classificação: %s\n", m.Rated)
	fmt.Printf("Sinopse:       %s\n", m.Plot)
	fmt.Println()
}

// Helper to print movies
func printMovies(ms []*pb.Movie) {
	for _, m := range ms {
		printMovie(m)
	}

	fmt.Printf("\n%d filme(s) encontrado(s).\n", len(ms))
	fmt.Println()
}
