#!/bin/bash

# Required parameters
USERNAME=$1
PASSWORD=$2
REPOSITORY=$3
README_FILEPATH=$4

# Authenticate with Docker Hub
TOKEN=$(curl -s -X POST -H "Content-Type: application/json" -d '{"username": "'${USERNAME}'", "password": "'${PASSWORD}'"}' https://hub.docker.com/v2/users/login/ | jq -r .token)

# Prepare the payload
README_PAYLOAD=$(<${README_FILEPATH} jq -sR .)

# Update the Docker Hub description
curl -s -X PATCH -H "Content-Type: application/json" -H "Authorization: JWT ${TOKEN}" -d "{\"full_description\": ${README_PAYLOAD}}" https://hub.docker.com/v2/repositories/${REPOSITORY}/

