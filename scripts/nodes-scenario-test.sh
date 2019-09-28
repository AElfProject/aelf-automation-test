#!/bin/bash

function Rand(){
    min=$1
    max=$(($2-$min+1))
    num=$(date +%s%N)
    echo $(($num%$max+$min))
}

function StartNode(){
    startid=$1
    waittime=$2

    nodename=""
    if [ ${startid} -gt 7 ]
    then
        echo "Wrong input parameter. node number is in [1,7]."
        exit 1
    elif [ ${startid} -gt 5 ]
    then
        nodename="full"${startid}
    else
        nodename="bp"${startid}
    fi

    sudo sh -c "echo '\nTEST - Start node ${nodename} at $(date)\n' >> ${BaseDir}/logs/${nodename}-aelf-node.log"
    cd ${BaseDir}/Node${startid}
    echo "Start node ${nodename} at $(date)"
    echo "dotnet AElf.Launcher.dll --config.path ${ConfigDir}/Config${startid} >> ${BaseDir}/logs/${nodename}-aelf-node.log"
    sudo sh -c "dotnet AElf.Launcher.dll --config.path ${ConfigDir}/Config${startid} >> ${BaseDir}/logs/${nodename}-aelf-node.log &"
    sleep ${waittime}
}

function StopNode(){
    stopid=$1

    nodename=""
    if [ ${startid} -gt 7 ]
    then
        echo "Wrong input parameter. node number is in [1,7]."
        exit 1
    elif [ ${startid} -gt 5 ]
    then
        nodename="full"${startid}
    else
        nodename="bp"${startid}
    fi
    echo "Stop node ${nodename} at $(date)"
    echo "sudo kill $(sudo ps -ef |grep dotnet |grep Config${stopid} |awk '{print $2}')"
    sudo kill $(sudo ps -ef |grep dotnet |grep Config${stopid} |awk '{print $2}')
    sudo sh -c "echo '\nT EST - Stop node ${stopid} at $(date +%s)\n' >> ${BaseDir}/logs/${nodename}-aelf-node.log"
}

function StopNodeCleanDB(){
    stopid=$1
    echo "Stop node ${nodename} and clean db at $(date)."
    StopNode ${stopid}
    sleep 30
    sudo redis-cli -n ${stopid} flushdb
}

function RemovePeers(){
    serviceUrl4="http://192.168.197.15:8040/net"
    serviceUrl5="http://192.168.197.15:8050/net"
    echo "Remove node4 peers."
    sudo sh -c "echo '\nTEST - Remove peers for node4 at $(date)\n' >> ${BaseDir}/logs/bp4-aelf-node.log"
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6810"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6820"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6830"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"get_peers","params":{},"id":0}' ${serviceUrl4}

    echo "\nRemove node5 peers."
    sudo sh -c "echo '\nTEST - Remove peers for node5 at $(date)\n' >> ${BaseDir}/logs/bp5-aelf-node.log"
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6810"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6820"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"remove_peer","params":{"address":"192.168.197.15:6830"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"get_peers","params":{},"id":0}' ${serviceUrl5}
}

function AddbackPeers(){
    serviceUrl4="http://192.168.197.15:8040/net"
    serviceUrl5="http://192.168.197.15:8050/net"
    echo "\nAdd back peers for node4."
    sudo sh -c "echo '\nTEST - Add back peers for node4 at $(date)\n' >> ${BaseDir}/logs/bp4-aelf-node.log"
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6810"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6820"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6830"},"id":1}' ${serviceUrl4}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"get_peers","params":{},"id":0}' ${serviceUrl4}

    echo "\nAdd back peers for node5."
    sudo sh -c "echo '\nTEST - Add back peers for node5 at $(date)\n' >> ${BaseDir}/logs/bp5-aelf-node.log"
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6810"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6820"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"add_peer","params":{"address":"192.168.197.15:6830"},"id":1}' ${serviceUrl5}
    echo ""
    curl -H "Content-Type:application/json" -H "Accept:application/json" -X POST --data '{"jsonrpc":"2.0","method":"get_peers","params":{},"id":0}' ${serviceUrl5}
    echo ""
}

function ScenarioTest(){
    nodeid=$(Rand 1 9)
    type=$(Rand 1 2)
    if [ ${nodeid} -gt 7 ]
    then
        echo "[TEST]: RemovePeers - AddPeers"
        RemovePeers
        sleep 120
        AddbackPeers
        sleep 180
    else
        if [ ${type} = 1 ]
        then
            echo "[TEST]: Stop - Restart"
            StopNode ${nodeid}
            sleep 120
            StartNode ${nodeid} 5
            sleep 180
        else
            echo "[TEST]: Stop - CleanDB - Restart"
            StopNodeCleanDB ${nodeid}
            sleep 120
            StartNode ${nodeid} 5
            sleep 180
        fi
    fi
}

# Program start
echo "Clean test db and dotnet process."
sudo redis-cli flushall
sudo kill -9 $(sudo ps -ef |grep dotnet |awk '{print $2}')

BaseDir='/home/aelf/github'
ConfigDir='/home/aelf/github/ConfigList'

echo "Testing BaseDir is: ${BaseDir}"

cd ${BaseDir}/AElf
echo "Current Branch=> `sudo git branch |grep "*"`"

read -p "Pull and build latest code(yes/no):" ANSWER
if [ ${ANSWER} = "yes" ]
then
    echo ">>Clean old aelf files"
    sudo mkdir -p ${BaseDir}/AElfRelease
    sudo rm -rf ${BaseDir}/AElfRelease/*
    echo ">>Pull and build aelf files"
    cd ${BaseDir}/AElf
    sudo git fetch
    sudo git pull
    sudo dotnet publish  --configuration Release -o ${BaseDir}/AElfRelease

    echo "Copy Release to other dirs"
    for ((i=1; i<=5; i ++))
    do
    sudo mkdir -p ${BaseDir}/Node${i}
    sudo rm -rf ${BaseDir}/Node${i}/*
    sudo cp -r ${BaseDir}/AElfRelease/* ${BaseDir}/Node${i}/
    done
else
    echo "No code updated, will run nodes directly."
fi

echo "Begin start nodes:"
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/bp1-aelf-node.log"
StartNode 1 20
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/bp2-aelf-node.log"
StartNode 2 5
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/bp3-aelf-node.log"
StartNode 3 5
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/bp4-aelf-node.log"
StartNode 4 5
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/bp5-aelf-node.log"
StartNode 5 5
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/full6-aelf-node.log"
StartNode 6 5
sudo sh -c "echo 'TEST - Testing Start\n' > ${BaseDir}/logs/full7-aelf-node.log"
StartNode 7 5

echo "All 7 nodes start completed. Will run rounds of scenario testing."
sleep 60

#Monitor stop and restart logic
for (( round=1; round<=100; round++ ))
do
    echo "Execute Times: "${round}
    ScenarioTest
done
echo "Complete all scenarios testing."