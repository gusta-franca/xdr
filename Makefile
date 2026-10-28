.PHONY: all clean down download restore setup up \
        proto proto-java proto-go proto-csharp \
        build-java build-go build-csharp build-proto clean-proto \
        protobuf-lib mongo-libs compile-java run-server run-client clean-java

DATA_DIR       := data
ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db

JAVA_DIR     := java
LIB_DIR      := lib
MAVEN_MONGO  := https://repo1.maven.org/maven2/org/mongodb
MONGO_VER    := 5.2.0

# Protocol Buffers
PROTO_DIR    := proto/
PROTO_FILE   := mflix.proto

JAVA_OUT     := javacode
JAVA_BUILD   := javacode/build
GO_OUT       := go/internal/pb
CSHARP_OUT   := csharpcode

PROTOC       ?= protoc
PROTOC_VER   := $(shell $(PROTOC) --version 2>/dev/null | sed 's/libprotoc //')
PROTOBUF_VER ?= $(shell echo "$(PROTOC_VER)" | awk -F. '{ if ($$1 >= 22) print "4." $$1 "." $$2; else print $$0 }')
PROTOBUF_JAR := $(JAVA_DIR)/$(LIB_DIR)/protobuf-java-$(PROTOBUF_VER).jar

JAVA_CP      := build:../$(JAVA_BUILD):$(LIB_DIR)/*

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
	$(PROTOC) --proto_path=$(PROTO_DIR) --java_out=$(JAVA_OUT) $(PROTO_FILE)

proto-go:
	mkdir -p $(GO_OUT)
	$(PROTOC) --proto_path=$(PROTO_DIR) --go_out=$(GO_OUT) --go_opt=paths=source_relative $(PROTO_FILE)

proto-csharp:
	mkdir -p $(CSHARP_OUT)
	$(PROTOC) --proto_path=$(PROTO_DIR) --csharp_out=$(CSHARP_OUT) $(PROTO_FILE)

# Compilação do código gerado
build-proto: build-java build-go build-csharp

build-java: proto-java protobuf-lib
	mkdir -p $(JAVA_BUILD)
	javac -cp $(PROTOBUF_JAR) -d $(JAVA_BUILD) $$(find $(JAVA_OUT) -name '*.java' -not -path '$(JAVA_BUILD)/*')

build-go: proto-go
	cd $(GO_OUT) && go mod tidy && go build ./...

# Requer um arquivo Mflix.Proto.csproj dentro de csharpcode/
build-csharp: proto-csharp
	cd $(CSHARP_OUT) && dotnet build

clean-proto:
	rm -rf $(JAVA_OUT) $(GO_OUT) $(CSHARP_OUT)

# Código Java do projeto (cliente e servidor)
protobuf-lib:
	mkdir -p $(JAVA_DIR)/$(LIB_DIR)
	wget -N -P $(JAVA_DIR)/$(LIB_DIR) https://repo1.maven.org/maven2/com/google/protobuf/protobuf-java/$(PROTOBUF_VER)/protobuf-java-$(PROTOBUF_VER).jar

mongo-libs:
	mkdir -p $(JAVA_DIR)/$(LIB_DIR)
	wget -N -P $(JAVA_DIR)/$(LIB_DIR) $(MAVEN_MONGO)/mongodb-driver-sync/$(MONGO_VER)/mongodb-driver-sync-$(MONGO_VER).jar
	wget -N -P $(JAVA_DIR)/$(LIB_DIR) $(MAVEN_MONGO)/mongodb-driver-core/$(MONGO_VER)/mongodb-driver-core-$(MONGO_VER).jar
	wget -N -P $(JAVA_DIR)/$(LIB_DIR) $(MAVEN_MONGO)/bson/$(MONGO_VER)/bson-$(MONGO_VER).jar
	wget -N -P $(JAVA_DIR)/$(LIB_DIR) $(MAVEN_MONGO)/bson-record-codec/$(MONGO_VER)/bson-record-codec-$(MONGO_VER).jar

compile-java: build-java
	mkdir -p $(JAVA_DIR)/build
	cd $(JAVA_DIR) && javac -cp "$(JAVA_CP)" -d build $$(find client common server -name '*.java')

run-server: compile-java
	cd $(JAVA_DIR) && java -cp "$(JAVA_CP)" server.Server

run-client: compile-java
	cd $(JAVA_DIR) && java -cp "$(JAVA_CP)" client.Client

clean-java:
	rm -rf $(JAVA_DIR)/build