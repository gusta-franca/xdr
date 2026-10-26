.PHONY: all clean down download restore setup up \
        proto proto-java proto-go proto-csharp \
        build-java build-go build-csharp build-proto clean-proto

DATA_DIR       := data
ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db

# Protocol Buffers
PROTO_DIR    := proto/
PROTO_FILE   := mflix.proto

JAVA_OUT     := javacode
JAVA_BUILD   := javacode/build
GO_OUT       := gocode
CSHARP_OUT   := csharpcode

PROTOBUF_JAR ?= /usr/share/java/protobuf.jar

# Para o protoc encontrar o plugin protoc-gen-go
export PATH := $(PATH):$(shell go env GOPATH 2>/dev/null)/bin

all: setup

clean:
	sudo docker compose down -v

down:
	sudo docker compose down

download:
	mkdir -p $(DATA_DIR)
	wget -N -P $(DATA_DIR) $(ARCHIVE_URL)

restore: up download
	sudo docker exec -i $(CONTAINER_NAME) mongorestore --archive --drop < $(ARCHIVE_FILE)

setup: restore proto build-proto

up:
	sudo docker compose up -d

# Geração de código
proto: proto-java proto-go proto-csharp

proto-java:
	mkdir -p $(JAVA_OUT)
	protoc --proto_path=$(PROTO_DIR) --java_out=$(JAVA_OUT) $(PROTO_FILE)

# Requer: go install google.golang.org/protobuf/cmd/protoc-gen-go@latest
proto-go:
	mkdir -p $(GO_OUT)
	protoc --proto_path=$(PROTO_DIR) --go_out=$(GO_OUT) --go_opt=paths=source_relative $(PROTO_FILE)

proto-csharp:
	mkdir -p $(CSHARP_OUT)
	protoc --proto_path=$(PROTO_DIR) --csharp_out=$(CSHARP_OUT) $(PROTO_FILE)

# Compilação do código gerado
build-proto: build-java build-go build-csharp

build-java: proto-java
	mkdir -p $(JAVA_BUILD)
	javac -cp $(PROTOBUF_JAR) -d $(JAVA_BUILD) $$(find $(JAVA_OUT) -name '*.java' -not -path '$(JAVA_BUILD)/*')

build-go: proto-go
	cd $(GO_OUT) && { [ -f go.mod ] || go mod init mflix/pb; } && go mod tidy && go build ./...

# Requer um arquivo Mflix.Proto.csproj dentro de csharpcode/
build-csharp: proto-csharp
	cd $(CSHARP_OUT) && dotnet build

clean-proto:
	rm -rf $(JAVA_OUT) $(GO_OUT) $(CSHARP_OUT)