.PHONY: clean down download restore setup up 

ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db
DATA_DIR       := data

all: setup

download:
	mkdir -p $(DATA_DIR)
	wget -nc -O $(ARCHIVE_FILE) $(ARCHIVE_URL)

up:
	sudo docker compose up -d

restore: up download
	sudo docker exec -i $(CONTAINER_NAME) mongorestore --archive --drop < $(ARCHIVE_FILE)

setup: restore

down:
	sudo docker compose down

clean: down
	rm -rf $(DATA_DIR)
