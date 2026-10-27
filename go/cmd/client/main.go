/*
   Data de criação: 24/10/2026
   Estudante: Gustavo Martins França
   Uso do cliente
*/

package main

import (
	"log"
	"os"

	"github.com/joho/godotenv"
)

func main() {
	err := godotenv.Load()

	if err != nil {
		log.Fatal("Error loading enviroment")
	}

	addr := os.Getenv("SERVER_ADDR")

	if addr == "" {
		log.Fatal("Provide a valid enviroment")
	}

	client := NewClient(addr)
	client.Run()
}
