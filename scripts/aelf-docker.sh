#!/bin/bash

echo "1. Delete AElfRelease files first"
sudo rm -rf /home/aelf/github/AElfRelease/*

echo "2. Pull and build project"
cd /home/aelf/github/AElf
read -p "Input branch name you want to build(default dev):" BRANCH
sudo git checkout $BRANCH
sudo git pull
sudo dotnet publish /p:NoBuild=false --configuration Release -o /home/aelf/github/AElfRelease

echo "3. Docker build image"
sudo docker build -t aelf/node:test /home/aelf/github/AElfRelease

echo "4. Login docker and push image to hub"
docker login -u=user -p=password
sudo docker push aelf/node:test

echo "5. Start aelf node"
PUBLISH_PORT='-p 6800:6800 -p 8000:8000'
BIND_VOLUME='-v /opt/node-1:/opt/node-1 -v /opt/node-1/keys:/root/.local/share/aelf/keys'
CONFIGURE_PATH='/opt/node-1'

if [[ "$1" = "start" ]];then
	docker run --name mycon --restart=always -itd ${PUBLISH_PORT}  ${BIND_VOLUME} -w  ${CONFIGURE_PATH} aelf/node:dev   dotnet /app/AElf.Launcher.dll
	[[ $? -eq 0 ]] && echo "start successful "
elif [[ "$1" = "stop" ]];then
	process=$(docker ps -a -q| wc -l)
	[[ ${process} -ge 1 ]] && docker rm -f `docker ps -a -q` || echo "Node not started";exit 1
	[[ $? -eq 0 ]] && echo "stop successful "
elif [[ "$1" = "restart" ]];then
	process=$(docker ps -a -q| wc -l)
	if [[ ${process} -ge 1 ]];then
	    docker rm -f `docker ps -a -q` && docker run --name mycon --restart=always -itd ${PUBLISH_PORT}  ${BIND_VOLUME} -w  ${CONFIGURE_PATH}  aelf/node:dev   dotnet /app/AElf.Launcher.dll
	else
	   docker run -itd --name mycon --restart=always ${PUBLISH_PORT}  ${BIND_VOLUME} -w  ${CONFIGURE_PATH}  aelf/node:dev dotnet /app/AElf.Launcher.dll
	fi
	[[ $? -eq 0 ]] && echo "restart successful"
elif [[ "$1" = "" ]];then
         docker run -itd --name mycon --restart=always ${PUBLISH_PORT}  ${BIND_VOLUME} -w  ${CONFIGURE_PATH} aelf/node:dev dotnet /app/AElf.Launcher.dll
	 [[ $? -eq 0 ]] && echo "start successful "
else
	echo " usage: $0 ''|start|stop|restart"
fi