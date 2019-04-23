#!/bin/bash

echo "1. Delete AElfRelease files first"
sudo rm -rf /home/aelf/github/AElfRelease/*

echo "2. Pull and build project"
cd /home/aelf/github/AElf
read -p "Input branch name you want to build(default dev):" BRANCH
sudo git checkout $BRANCH
sudo git pull
sudo dotnet publish -nowarn:CS0108,CS0162,CS0168,CS0169,CS0219,CS0414,CS0649,CS0659,CS1998,CS2002,CS4014,NU1603,NU1701,MSB3245,MSB3026,xUnit1013,xUnit2000,xUnit2000,xUnit2002,xUnit2009,xUnit2013,xUnit2017 --configuration Release -o /home/aelf/github/AElfRelease

echo "3. Docker build image"
sudo docker build -t aelf/node:test /home/aelf/github/AElfRelease

echo "4. Login docker and push image to hub"
docker login -u=user -p=password
sudo docker push aelf/node:test