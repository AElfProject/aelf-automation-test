#!/bin/bash
echo "Clean test db environment"
sudo redis-cli flushdb
BaseDir='/home/aelf'
read -p "Input test folder(default is /home/aelf):" InputDir
if [ -n "$InputDir" -a -d "$InputDir" ]
then
	echo "Use Input testing dir"
	BaseDir=$InputDir
fi
echo "Testing BaseDir is: $BaseDir"

read -p "Pull and build latest code(yes/no):" ANSWER
if [ $ANSWER = "yes" ]
then
	echo ">>Clean old aelf files"
	sudo mkdir -p $BaseDir/github/AElfRelease
	sudo rm -rf $BaseDir/github/AElfRelease/*
	echo ">>Pull and build aelf files"
	cd $BaseDir/github/AElf
	sudo git pull
	sudo dotnet restore
	sudo dotnet build
	sudo dotnet publish --configuration Release -o $BaseDir/github/AElfRelease
else
	echo "No code updated."
fi

echo "Update contracts file for testing"
sudo cp $BaseDir/github/AElfRelease/AElf.Kernel.Tests.TestContract.dll $BaseDir/contracts
sudo cp $BaseDir/github/AElfRelease/AElf.Contracts.Token.dll $BaseDir/contracts
sudo cp $BaseDir/github/AElfRelease/AElf.Benchmark.TestContract.dll $BaseDir/contracts

echo "Prepare account keys to run node"
echo "Get current user account information"
UserName=`whoami`
echo ">>Delete old test data"
sudo rm -rf /home/$UserName/.local/share/aelf/keys/*
sudo rm -rf /home/$UserName/.local/share/aelf/contracts/*
echo ">>Copy new test data"
sudo cp $BaseDir/keys/* /home/$UserName/.local/share/aelf/keys
sudo cp $BaseDir/contracts/* /home/$UserName/.local/share/aelf/contracts
sudo cp -r $BaseDir/config /home/$UserName/.local/share/aelf

echo ">>Select account"
dirpath="/home/$UserName/.local/share/aelf/keys"
for account_file in ${dirpath}/*
do
    ACCOUNT=`basename $account_file .ak`
    break
done
echo "  >> Run node with account: $ACCOUNT"

echo "Begin execute dotnet command to run node"
cd $BaseDir/github/AElfRelease
read -p "Select run type(main=1, other=2):" RunType
if [ $RunType = 1 ]
then
	echo "Run MAIN node with command:"
	echo "sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8000 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 0 --node.account $ACCOUNT --node.port 6800 --dpos.generator true --chain.new true"
	sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8000 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 0 --node.account $ACCOUNT --node.port 6800 --dpos.generator true --chain.new true
else
	echo "Run OTHER node with command:"
	read -p "Input main chain chain id: " ChainID
	sudo sh -c "echo '{\n \"id\":\"$ChainID\"\n}' > $BaseDir/github/AElfRelease/ChainInfo.json"
	read -p "Input main node rpc info like(192.168.197.34:6800):" MainNode 
	echo "sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8000 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 0 --node.account $ACCOUNT --node.port 6800 --bootnodes $MainNode"
	sudo dotnet AElf.Launcher.dll --mine.enable true --rpc.port 8000 --rpc.host 0.0.0.0 --db.type redis --db.host 127.0.0.1 --db.port 6379 --db.number 0 --node.account $ACCOUNT --node.port 6800 --bootnodes $MainNode
fi
