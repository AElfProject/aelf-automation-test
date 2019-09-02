#!/bin/bash

echo "1. Delete old test resource"
sudo mkdir -p /home/aelf/test/TestResult
sudo mkdir -p /home/aelf/test/TestRelease
sudo rm -rf /home/aelf/test/TestResult/*
sudo rm -rf /home/aelf/test/TestRelease/*

echo "2. Pull and build automation project"
cd /home/aelf/github/AElf.Automation
sudo git pull
sudo dotnet restore
sudo dotnet build
sudo dotnet publish --configuration Release -o /home/aelf/test/TestRelease

echo "3. Copy keys and contract into TestRelease"
sudo mkdir -p /home/aelf/test/TestRelease/aelf
sudo cp -r /home/aelf/keys /home/aelf/test/TestRelease/aelf
sudo cp -r /home/aelf/contracts /home/aelf/test/TestRelease/aelf

echo "4. Update AccountInfo.json"
dirpath="/home/aelf/test/TestRelease/aelf/keys"
count=0
for account_file in ${dirpath}/*
do
    let count++
    if [ $count = 2 ]
    then
        ACCOUNT=`basename $account_file .json`
        break
    fi
done
echo "  >> Run automation with account: $ACCOUNT"
sudo sh -c "echo '{\n \"account\":\"$ACCOUNT\"\n}' > /home/aelf/test/TestRelease/AccountInfo.json"

read -p "Begin Execute auto tesing? " BEGIN

echo "5. Run AElf automation test cases"
sudo docker exec -it aelf-node-launcher dotnet vstest /app/aelf/test/TestRelease/AElf.Automation.CliTesting.dll --logger:"trx;LogFileName=CliAutoMation.trx" --ResultsDirectory:/app/aelf/test/TestResult

echo "6. Complete CLI automation test."