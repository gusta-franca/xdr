/*
   Data de criação: 24/10/2026
   Estudante: Gustavo Martins França
   Uso do servidor
*/

package main

import (
	"context"
	"log"
	"os"

	"github.com/joho/godotenv"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"

	"mflix/internal/repository"
)

func main() {
	err := godotenv.Load()

	if err != nil {
		log.Fatal("Error loading enviroment")
	}

	addr := os.Getenv("SERVER_ADDR")
	mongoURI := os.Getenv("MONGODB_URI")
	dbName := os.Getenv("MONGODB_DB")
	collName := os.Getenv("MONGODB_COLLECTION")

	if addr == "" || mongoURI == "" || dbName == "" || collName == "" {
		log.Fatal("Provide a valid enviroment")
	}

	client, err := mongo.Connect(context.Background(), options.Client().ApplyURI(mongoURI))

	if err != nil {
		log.Fatalf("Error connecting to MongoDB: %s", err)
	}

	defer client.Disconnect(context.Background())

	movieRepository := repository.NewMovieRepository(client.Database(dbName).Collection(collName))

	server := NewServer(addr, movieRepository)
	server.Run()
}
