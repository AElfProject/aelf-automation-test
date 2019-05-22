#!/bin/bash

BASE_PATH=$(cd `dirname $0`;pwd)
cd ${BASE_PATH}
cd ..
CodePath=`pwd`

echo "Get contract test path"
OsType=`uname`
UserName=`whoami`
ContractPath=""
if [[ ${OsType} = "Darwin" ]]
then
    ContractPath=/Users/${UserName}/.local/share/aelf/contracts
else
    ContractPath=/home/${UserName}/.local/share/aelf/contracts
fi
mkdir -p ${ContractPath}
echo "Contract path: ${ContractPath}"
echo ""

read -p "Pull and build latest code(yes/no):" ANSWER
if [[ ${ANSWER} = "yes" ]]
then
    echo "=> Pull latest build"
    rm -rf $(find ./ -name "*.g.cs")
    rm -rf $(find ./ -name "*.c.cs")
    git pull
    git submodule update
    echo ""
    
    echo "=> Clean old files"
    mkdir -p ${CodePath}/TestRelease
    rm -rf ${CodePath}/TestRelease/*
    
    echo "=> Build token contract"
    cd ${CodePath}/src/AElf/src/AElf.Contracts.MultiToken
    dotnet build /p:NoBuild=false --configuration Release -o ${CodePath}/TestRelease
    
    echo "=> Copy contract file"
    cp ${CodePath}/TestRelease/AElf.Contracts.MultiToken.dll ${ContractPath}
    echo ""
    
    echo "=> Build automation test tool"
    cd ${CodePath}
    dotnet build /p:NoBuild=false --configuration Release -o ${CodePath}/TestRelease
    echo ""
fi

read -p "Provide test arguments to run automation(eg: -tc 1, -tg 4 -ru http://localhost:8000 -em 4):" Parameters
cd ${CodePath}/TestRelease
dotnet AElf.Automation.RpcPerformance.dll ${Parameters}