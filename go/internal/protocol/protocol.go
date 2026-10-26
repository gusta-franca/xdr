/*
   Data de criação: 26/10/2026
   Estudante: Gustavo Martins França
   Definição do protocolo comum entre linguagens
*/

package protocol

import (
	"encoding/binary"
	"io"

	"google.golang.org/protobuf/proto"
)

type Protocol struct {
	rw io.ReadWriter
}

// Protocol constructor
func NewUProtocol(rw io.ReadWriter) *Protocol {
	return &Protocol{
		rw: rw,
	}
}

// Reads a big-endian header with 4 bytes, then reads the bytes informed in the header
func (p *Protocol) ReadMessage(message proto.Message) error {
	header := make([]byte, 4)

	// reads header
	if _, err := io.ReadFull(p.rw, header); err != nil {
		return err
	}

	length := binary.BigEndian.Uint32(header)
	data := make([]byte, length)

	// reads data
	if _, err := io.ReadFull(p.rw, data); err != nil {
		return err
	}

	return proto.Unmarshal(data, message)
}

// Writes a big endian header with 4 bytes, then writes the body
func (p *Protocol) WriteMessage(message proto.Message) error {
	buffer, err := proto.Marshal(message)

	if err != nil {
		return err
	}

	dataLen := uint32(len(buffer))
	data := make([]byte, 4+dataLen)

	binary.BigEndian.PutUint32(data[:4], dataLen)
	copy(data[4:], buffer)

	// writes header + message
	_, err = p.rw.Write(data)
	return err
}
