#!/bin/bash

echo "1. Delete AElfRelease files first"
sudo rm -rf /home/aelf/github/AElfRelease/*

echo "2. Pull and build project"
cd /home/aelf/github/AElf
read -p "Input branch name you want to build(default dev):" BRANCH
sudo git checkout $BRANCH
sudo git pull
sudo dotnet publish --configuration Release -o /home/aelf/github/AElfRelease

echo "3. Docker build image"
sudo docker build -t aelf/node:test /home/aelf/github/AElfRelease

echo "4. Login docker and push image to hub"
docker login -u=[user] -p=[password]
sudo docker push aelf/node:test