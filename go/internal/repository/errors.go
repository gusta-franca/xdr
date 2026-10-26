/*
   Data de criação: 26/10/2026
   Estudante: Gustavo Martins França
   Erros para o pacote repository; refletem os erros definidos em mflix.proto
*/

package repository

import "errors"

var (
	ErrNotFound  = errors.New("movie not found")
	ErrInvalidID = errors.New("malformed id")
)
