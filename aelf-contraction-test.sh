#!/bin/bash

sudo mkdir -p /home/aelf/github/AutomationRelease

read -p "Update Testing code or not(yes/no):" UPDATE
if [ $UPDATE == "yes" ]
then
    echo ">Update AElf code"
    cd /home/aelf/github/aelf-automation-test/AElf
    echo "AElf Branch=> `sudo git branch |grep "*"`"
    sudo rm -rf $(sudo find ./ -name *.g.cs)
    sudo git pull
    echo ">Update Test code"
    cd /home/aelf/github/aelf-automation-test/AElf.Automation.RpcPerformance
    echo "Automation Branch=> `sudo git branch |grep "*"`"
    sudo git pull
    echo ">Build Test code"
    sudo dotnet build --configuration Release -o /home/aelf/github/AutomationRelease
fi
16
echo "Run automation testing."
cd /home/aelf/github/AutomationRelease
read -p "Input testing parameters(like: 4 10 http://192.168.197.34:8000/chain):" PARAMETER
sudo dotnet AElf.Automation.RpcPerformance.dll $PARAMETER