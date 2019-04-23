#!/bin/bash
echo "Check port parameter ..."
PORT=$1
if [ ! -n "$PORT" ]
then
    PORT=8000
    echo "  >> Use default port value: 8000"
else
    echo "  >> Use input port value: $PORT"
fi

echo "Sync time info ..."
sudo ntpdate cn.pool.ntp.org

echo "Pull latest aelf/node image ..."
sudo docker pull aelf/node:test

echo "Delete container if exist ..."
process=`sudo docker ps -a | grep aelf-node-cli | grep -v grep | grep -v sudo | wc -l`
if [ $process -eq 1 ]
then
    echo "  >> Stop container ..."
    sudo docker stop aelf-node-cli

    echo "  >> Delete existed container ..."
    sudo docker rm aelf-node-cli
fi

process=`sudo docker ps | grep aelf-node-launcher | grep -v grep | grep -v sudo | wc -l`
if [ $process -eq 1 ]
then
    echo "Start cli & connect to aelf node .."
    echo "Execute docker command: sudo docker exec -it aelf-node-launcher dotnet AElf.CLI.dll http://127.0.0.1:$PORT"
    sudo docker exec -it aelf-node-launcher dotnet AElf.CLI.dll http://127.0.0.1:$PORT
    exit 0
fi

echo "Start cli (not connect node)..."
echo "Execute docker command: sudo docker run -it -v /home/aelf:/app/aelf --name aelf-node-cli aelf/node:test dotnet AElf.CLI.dll http://127.0.0.1:$PORT"
sudo docker run -it -v /home/aelf:/app/aelf --name aelf-node-cli aelf/node:test dotnet AElf.CLI.dll http://127.0.0.1:$PORT