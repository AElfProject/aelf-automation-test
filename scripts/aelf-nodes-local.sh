#!/bin/bash
echo "Clean test db environment"
sudo redis-cli flushdb
sudo kill -9 $(sudo ps -ef |grep dotnet |awk '{print $2}')

BaseDir='/home/aelf'
read -p "Input test folder(default is /home/aelf):" InputDir
if [ -n "$InputDir" -a -d "$InputDir" ]
then
	BaseDir=$InputDir
fi
echo "Testing BaseDir is: $BaseDir"
cd $BaseDir/github/AElf
echo "Current Branch=> `sudo git branch |grep "*"`"

read -p "Pull and build latest code(yes/no):" ANSWER
if [ $ANSWER = "yes" ]
then
	echo ">>Clean old aelf files"
	sudo mkdir -p $BaseDir/github/AElfRelease
	sudo rm -rf $BaseDir/github/AElfRelease/*
	echo ">>Pull and build aelf files"
	cd $BaseDir/github/AElf
	sudo git pull
	sudo dotnet publish -nowarn:CS0108,CS0162,CS0168,CS0169,CS0219,CS0414,CS0649,CS0659,CS1998,CS2002,CS4014,NU1603,NU1701,MSB3245,MSB3026,xUnit1013,xUnit2000,xUnit2000,xUnit2002,xUnit2009,xUnit2013,xUnit2017 --configuration Release -o $BaseDir/github/AElfRelease
else
	echo "No code updated."
fi

echo "Update contracts file for testing"
sudo cp $BaseDir/github/AElfRelease/AElf.Kernel.Tests.TestContract.dll $BaseDir/contracts
sudo cp $BaseDir/github/AElfRelease/AElf.Contracts.Token.dll $BaseDir/contracts
sudo cp $BaseDir/github/AElfRelease/AElf.Benchmark.TestContract.dll $BaseDir/contracts

echo "Prepare account keys to run nodes"
echo "Get current user account information"
UserName=`whoami`
echo ">>Delete old test data"
sudo rm -rf /home/$UserName/.local/share/aelf/keys/*
sudo rm -rf /home/$UserName/.local/share/aelf/contracts/*
echo ">>Copy new test data"
sudo mkdir -p /home/$UserName/.local/share/aelf/keys
sudo mkdir -p /home/$UserName/.local/share/aelf/config
sudo mkdir -p /home/$UserName/.local/share/aelf/contracts

sudo cp $BaseDir/keys/* /home/$UserName/.local/share/aelf/keys
sudo cp -r $BaseDir/config/* /home/$UserName/.local/share/aelf/config
sudo cp $BaseDir/contracts/* /home/$UserName/.local/share/aelf/contracts

cd /home/$UserName/.local/share/aelf/config
sudo mv miners3.json miners.json 

echo ">>Select account"
dirpath="/home/$UserName/.local/share/aelf/keys"
declare -a AccountList
count=0
for account_file in ${dirpath}/*
do
	AccountList[count]=`basename $account_file .json`
	let count++
done
echo "  >> Run nodes with account:"
for acc in ${AccountList[@]}
do
    echo $acc
done

echo "Copy Release to other dirs"
sudo mkdir -p $BaseDir/github/Node1
sudo mkdir -p $BaseDir/github/Node2
sudo mkdir -p $BaseDir/github/Node3
sudo mkdir -p $BaseDir/github/logs

sudo rm -rf $BaseDir/github/Node1/*
sudo rm -rf $BaseDir/github/Node2/*
sudo rm -rf $BaseDir/github/Node3/*
sudo rm -rf $BaseDir/github/logs/*

sudo cp -r $BaseDir/github/AElfRelease/* $BaseDir/github/Node1/
sudo cp -r $BaseDir/github/AElfRelease/* $BaseDir/github/Node2/
sudo cp -r $BaseDir/github/AElfRelease/* $BaseDir/github/Node3/

cd $BaseDir/github/Node1
echo "Run MAIN node with command:"
echo "sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8100 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 1 --node.account ${AccountList[0]} --node.accountpassword 123 --node.port 6810 --dpos.generator true --chain.new true"
sudo sh -c "dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8100 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 1 --node.account ${AccountList[0]} --node.accountpassword 123 --node.port 6810 --dpos.generator true --chain.new true > $BaseDir/github/logs/main-aelf-node.log &"
sleep 5s

cd $BaseDir/github/Node2
echo "Run OTHER node1 with command:"
sudo cp $BaseDir/github/Node1/ChainInfo.json $BaseDir/github/Node2/ 
echo "sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8200 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 2 --node.account ${AccountList[1]} --node.accountpassword 123 --node.port 6820 --bootnodes 127.0.0.1:6810"
sudo sh -c "dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8200 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 2 --node.account ${AccountList[1]} --node.accountpassword 123 --node.port 6820 --bootnodes 127.0.0.1:6810 > $BaseDir/github/logs/other1-aelf-node.log &"
sleep 5s

cd $BaseDir/github/Node3
echo "Run OTHER node2 with command:"
sudo cp $BaseDir/github/Node1/ChainInfo.json $BaseDir/github/Node3/ 
echo "sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8300 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 3 --node.account ${AccountList[2]} --node.accountpassword 123 --node.port 6830 --bootnodes 127.0.0.1:6810 127.0.0.1:6820"
sudo sh -c "dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8300 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 3 --node.account ${AccountList[2]} --node.accountpassword 123 --node.port 6830 --bootnodes 127.0.0.1:6810 127.0.0.1:6820 > $BaseDir/github/logs/other2-aelf-node.log &"

echo "All three nodes launch completed."