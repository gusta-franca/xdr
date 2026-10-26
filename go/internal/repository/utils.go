/*
   Data de criação: 24/10/2026
   Estudante: Gustavo Martins França
   Utilidades para o pacote repository
*/

package repository

import (
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"

	"mflix/internal/pb"
)

// parses a given id from hex code
func getID(id string) (primitive.ObjectID, error) {
	oid, err := primitive.ObjectIDFromHex(id)

	if err != nil {
		return primitive.NilObjectID, ErrInvalidID
	}

	return oid, nil
}

// helper to assert a know string field's type to be string
func str(m bson.M, k string) string {
	s, _ := m[k].(string)
	return s
}

// helper to assert a know []string field's types to be string
func strs(m bson.M, k string) []string {
	arr, _ := m[k].(primitive.A) // arr == []interface{}
	buffer := make([]string, 0, len(arr))

	for _, v := range arr {
		if s, ok := v.(string); ok {
			buffer = append(buffer, s)
		}
	}

	return buffer
}

// helper to assert a know numeric field's type
func num(m bson.M, k string) int32 {
	switch n := m[k].(type) {
	case int32:
		return n

	case int64:
		return int32(n)

	case float64:
		return int32(n)

	case string:
		var x int32

		for _, c := range n {
			if c < '0' || c > '9' {
				break
			}

			x = x*10 + int32(c-'0')
		}

		return x
	}

	return 0
}

func toProto(m bson.M) *pb.Movie {
	released := ""

	if dt, ok := m["released"].(primitive.DateTime); ok {
		released = dt.Time().UTC().Format(time.RFC3339)
	}

	oid, _ := m["_id"].(primitive.ObjectID)

	return &pb.Movie{
		Id:               oid.Hex(),
		Plot:             str(m, "plot"),
		Genres:           strs(m, "genres"),
		Runtime:          num(m, "runtime"),
		Cast:             strs(m, "cast"),
		NumMflixComments: num(m, "num_mflix_comments"),
		Title:            str(m, "title"),
		Fullplot:         str(m, "fullplot"),
		Countries:        strs(m, "countries"),
		Released:         released,
		Directors:        strs(m, "directors"),
		Rated:            str(m, "rated"),
		Lastupdated:      str(m, "lastupdated"),
		Year:             num(m, "year"),
		Type:             str(m, "type"),
	}
}

func fromProto(m *pb.Movie) bson.M {
	doc := bson.M{
		"plot":               m.Plot,
		"genres":             m.Genres,
		"runtime":            m.Runtime,
		"cast":               m.Cast,
		"num_mflix_comments": m.NumMflixComments,
		"title":              m.Title,
		"fullplot":           m.Fullplot,
		"countries":          m.Countries,
		"directors":          m.Directors,
		"rated":              m.Rated,
		"lastupdated":        m.Lastupdated,
		"year":               m.Year,
		"type":               m.Type,
	}

	if t, err := time.Parse(time.RFC3339, m.Released); err == nil {
		doc["released"] = t
	}

	return doc
}
