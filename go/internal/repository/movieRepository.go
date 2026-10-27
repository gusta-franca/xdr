/*
   Data de criação: 24/10/2026
   Estudante: Gustavo Martins França
   Definição do acesso (leitura/escrita) à coleção (relação?) movies do banco de dados sample_mflix
*/

package repository

import (
	"context"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"

	"mflix/internal/pb"
)

type MovieRepository struct {
	coll *mongo.Collection
}

// MovieRepository constructor
func NewMovieRepository(coll *mongo.Collection) *MovieRepository {
	return &MovieRepository{
		coll: coll,
	}
}

// Inserts a movie and returns it with the generated id
func (r *MovieRepository) Create(ctx context.Context, m *pb.Movie) (*pb.Movie, error) {
	res, err := r.coll.InsertOne(ctx, fromProto(m))

	if err != nil {
		return nil, err
	}

	m.Id = res.InsertedID.(primitive.ObjectID).Hex()

	return m, nil
}

// Finds a movie by id
func (r *MovieRepository) Get(ctx context.Context, id string) (*pb.Movie, error) {
	oid, err := getID(id)

	if err != nil {
		return nil, err
	}

	var doc bson.M
	err = r.coll.FindOne(ctx, bson.M{"_id": oid}).Decode(&doc)

	if err == mongo.ErrNoDocuments {
		return nil, ErrNotFound
	}

	if err != nil {
		return nil, err
	}

	return toProto(doc), nil
}

// Overwrites the fields of an existing movie and returns the stored version
func (s *MovieRepository) Update(ctx context.Context, m *pb.Movie) (*pb.Movie, error) {
	oid, err := getID(m.Id)

	if err != nil {
		return nil, err
	}

	res, err := s.coll.UpdateOne(ctx, bson.M{"_id": oid}, bson.M{"$set": fromProto(m)})

	if err != nil {
		return nil, err
	}

	if res.MatchedCount == 0 {
		return nil, ErrNotFound
	}

	return s.Get(ctx, m.Id)
}

// Removes a movie by id
func (s *MovieRepository) Delete(ctx context.Context, id string) error {
	oid, err := getID(id)

	if err != nil {
		return err
	}

	res, err := s.coll.DeleteOne(ctx, bson.M{"_id": oid})

	if err != nil {
		return err
	}

	if res.DeletedCount == 0 {
		return ErrNotFound
	}

	return nil
}

// Lists all movies by an actor name
func (s *MovieRepository) ListByActor(ctx context.Context, actor string) ([]*pb.Movie, error) {
	return s.list(ctx, bson.M{"cast": actor})
}

// Lists all movies by a genre name
func (s *MovieRepository) ListByGenre(ctx context.Context, genre string) ([]*pb.Movie, error) {
	return s.list(ctx, bson.M{"genres": genre})
}

// Lists all movies; filter is optional
func (s *MovieRepository) list(ctx context.Context, filter bson.M) ([]*pb.Movie, error) {
	cur, err := s.coll.Find(ctx, filter)

	if err != nil {
		return nil, err
	}

	var docs []bson.M

	if err := cur.All(ctx, &docs); err != nil {
		return nil, err
	}

	movies := make([]*pb.Movie, 0, len(docs))

	for _, d := range docs {
		movies = append(movies, toProto(d))
	}

	return movies, nil
}
