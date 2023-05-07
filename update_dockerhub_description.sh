#!/bin/bash

set -e

USERNAME=$1
PASSWORD=$2
REPOSITORY=$3
README_FILEPATH=$4

# Obtain a token from Docker Hub
TOKEN=$(curl -s -X POST -H "Content-Type: application/json" -d '{"username": "'${USERNAME}'", "password": "'${PASSWORD}'"}' https://hub.docker.com/v2/users/login/ | jq -r .token)

# Get the current README content from the file
README_CONTENT=$(<$README_FILEPATH)

# Update the Docker Hub repository description
curl -s -X PATCH -H "Content-Type: application/json" -H "Authorization: JWT ${TOKEN}" -d '{"full_description": "'"${README_CONTENT//$'\n'/'\n'}"'"}' "https://hub.docker.com/v2/repositories/${REPOSITORY}/"

echo "Docker Hub description updated successfully!"