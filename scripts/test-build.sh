#!/bin/bash

BASE_PATH=$(cd `dirname $0`;pwd)
cd ${BASE_PATH}
cd ..
CodePath=`pwd`

UserName=`whoami`
ContractPath=/home/${UserName}/.local/share/aelf/contracts
sudo mkdir -p ${ContractPath}

read -p "Pull and build latest code(yes/no):" ANSWER
if [[ ${ANSWER} = "yes" ]]
then
    sudo rm -rf $(find ./ -name "*.g.cs")
    sudo rm -rf $(find ./ -name "*.c.cs")
    sudo git pull
    sudo git submodule update
    
    echo "=> Clean old files"
    sudo mkdir -p ${CodePath}/TestRelease
    sudo rm -rf ${CodePath}/TestRelease/*
    
    echo "=> Build token contract"
    cd ${CodePath}/src/AElf/src/AElf.Contracts.MultiToken
    sudo dotnet build /p:NoBuild --configuration Release -o ${CodePath}/TestRelease
    
    echo "=> Copy contract file"
    sudo cp ${CodePath}/TestRelease/AElf.Contracts.MultiToken.dll ${ContractPath}
    
    echo "=> Build automation test tool"
    cd ${CodePath}
    sudo dotnet build /p:NoBuild --configuration Release -o ${CodePath}/TestRelease
fi

read -p "Provide test arguments to run automation(eg: -tc 1, -tg 4 -ru http://localhost:8000 -em 4):" Parameters
cd ${CodePath}/TestRelease
sudo dotnet AElf.Automation.RpcPerformance.dll ${Parameters}


