.PHONY: clean down download restore setup up 

DATA_DIR       := data
ARCHIVE_FILE   := $(DATA_DIR)/sampledata.archive
ARCHIVE_URL    := https://atlas-education.s3.amazonaws.com/sampledata.archive
CONTAINER_NAME := mflix-db

# add more ocmmands if needed
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

setup: restore

up:
	sudo docker compose up -d
