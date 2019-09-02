#!/bin/bash

echo "Pull latest aelf/node image ..."
sudo docker pull aelf/node:test

echo "Delete container if exist ..."
process=`sudo docker ps -a | grep aelf-node-launcher | grep -v grep | grep -v sudo | wc -l`
if [ $process -eq 1 ]
then
    echo "  >> Stop container"
    sudo docker stop aelf-node-launcher
    echo "  >> Delete existed container"
    sudo docker rm aelf-node-launcher
fi

echo "Sync time info ..."
sudo apt-get install ntpdate
sudo ntpdate cn.pool.ntp.org

echo "Check parameter ..."
ACCOUNT=$1
if [ ! -n "$ACCOUNT" ]
then
    dirpath="/home/aelf/keys"
    for account_file in ${dirpath}/*
    do
        ACCOUNT=`basename $account_file .json`
        break
    done
fi
echo "  >> Run node with account: $ACCOUNT"

PORT=$2
if [ ! -n "$PORT" ]
then
    PORT=8000
    echo "  >> Use default port value: 8000"
else
    echo "  >> Use input port value as: $PORT"
fi

echo "Check docker environment ..."
app=`sudo docker -v |grep version |wc -l`
if [ $app -eq 1 ]
then
    echo "  >> Docker installed."
else
    echo "  >> Error: Docker command not install."
    return
fi

echo "Start launcher container, select run type:"
echo "1. Start new main node: smn"
echo "2. Restart main node: rmn"
echo "3. Start other node: son"
echo "4. Restart other node: ron"
while read -p "Input run type(smn/rmn/son/ron): " RunType
do
if [ $RunType = "smn" ]
then
    echo " >> Start main node:"
	echo ">> Delete log files and flush db."
	sudo rm -rf /tmp/logs
	sudo redis-cli flushdb
	echo "Execute docker command: sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --dpos.generator true --chain.new true"
	sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --dpos.generator true --chain.new true
	break
elif [ $RunType = "rmn" ]
then
	echo ">> Delete log files."
	sudo rm -rf /tmp/logs
	echo ">>Restart main node:"
	echo "Execute docker command: sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800--dpos.generator true"
	sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --dpos.generator true
	break
elif [ $RunType = "son" ]
then
	echo ">> Delete log files and flush db."
	sudo rm -rf /tmp/logs
	sudo redis-cli flushdb
	echo ">> Start other node:"
	echo "Update ChainId info for connector node"
	read -p "Input main chain chain id: " ChainID
	sudo sh -c "echo '{\n \"id\":\"$ChainID\"\n}' > /home/aelf/ChainInfo.json"
	
	read -p "Input main node rpc info like(192.168.197.34:6800):" MainNode
	echo "Execute docker command: sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --bootnodes $MainNode"
	sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --bootnodes $MainNode
	break
elif [ $RunType = "ron" ]
then
	echo ">> Delete log files."
	sudo rm -rf /tmp/logs
	echo ">> Restart other node:"
	read -p "Input main node rpc info like(192.168.197.34:6800):" MainNode
	echo "Execute docker command: sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --bootnodes $MainNode"
	sudo docker run -it -p 6800:6800 -p $PORT:$PORT -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json -v /tmp/logs:/app/logs --name aelf-node-launcher aelf/node:test dotnet AElf.Launcher.dll --mine.enable true --rpc.port $PORT --rpc.host 0.0.0.0 --db.type redis --db.host 172.17.0.1 --db.port 6379 --node.account $ACCOUNT --node.accountpassword 123 --node.port 6800 --bootnodes $MainNode
	break
else
	echo "Wrong input selection, input again."
fi
done