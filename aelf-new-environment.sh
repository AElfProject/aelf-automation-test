#!/bin/bash
function echogreen(){
	MSG=$1
    COLOER=`echo $1|sed 's#^.*\[\(.*\)\].*#\1#g'`
    BASE=`echo $1|sed 's#\(^.*\)\[.*]#\1#g'`
	echo -e "${BASE} [\e[0;31;1m  $COLOER  \e[0m]"
}

function echored(){
    MSG=$1
    COLOER=`echo $1|sed 's#^.*\[\(.*\)\].*#\1#g'`
    BASE=`echo $1|sed 's#\(^.*\)\[.*]#\1#g'`
	echo -e "${BASE} [\e[1;32m $COLOER \e[0m]"
}

echogreen "Create dir"
sudo mkdir -p /opt/protoc
sudo mkdir -p /home/aelf/keys
sudo mkdir -p /home/aelf/config
sudo mkdir -p /home/aelf/contracts
sudo mkdir -p /home/aelf/scripts
sudo mkdir -p /home/aelf/github
sudo mkdir -p /home/aelf/github/AElfRelease
sudo mkdir -p /home/aelf/others

echogreen "Update env"
sudo apt-get update
sudo apt-get install unzip

echogreen "Install redis"
sudo apt-get install redis-server -y

echogreen "Install nginx"
sudo apt-get install nginx -y

echogreen "Install Mysql"
sudo apt-get install mysql-server -y

echogreen "Install protoc"
cd /home/aelf/others
sudo wget https://github.com/google/protobuf/releases/download/v3.6.0/protoc-3.6.0-linux-x86_64.zip
sudo unzip protoc-3.6.0-linux-x86_64.zip -d /opt/protoc
sudo ln -s /opt/protoc/bin/protoc /usr/bin/protoc

echogreen "Install dotnet"
sudo wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get install apt-transport-https -y
sudo apt-get update
sudo apt-get install dotnet-sdk-2.1 -y

echogreen "Install docker"
sudo apt-get remove docker docker-engine docker.io
sudo apt-get install \
    apt-transport-https \
    ca-certificates \
    curl \
    software-properties-common -y
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
sudo apt-key fingerprint 0EBFCD88
sudo add-apt-repository \
   "deb [arch=amd64] https://download.docker.com/linux/ubuntu \
   $(lsb_release -cs) \
   stable"
sudo apt-get update
sudo apt-get install docker-ce -y

echogreen "Pull aelf and test code"
cd /home/aelf/github
sudo git clone https://github.com/AElfProject/AElf
sudo git clone https://github.com/AElfProject/aelf-web-wallet
sudo git clone https://github.com/AElfProject/aelf-automation-test --recursive

echogreen "Build and copy files"
cd /home/aelf/github/AElf
sudo dotnet publish --configuration Release -o /home/aelf/github/AElfRelease
sudo cp /home/aelf/github/AElfRelease/config/* /home/aelf/config/

cd /home/aelf/github/aelf-automation-test
sudo cp *.sh /home/aelf/scripts
sudo chmod -R 777 /home/aelf/scripts/

echogreen "Complete test vm environment preparation."