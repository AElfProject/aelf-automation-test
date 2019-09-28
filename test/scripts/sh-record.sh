#! /bin/bash

read -p "Please input the session filename you want to create: " filename

sesfile="$filename.session"
logfile="$filename.timing.log"

if [ -e $sesfile ]
then
    echo "$sesfile is Exsit, Create sassion file fault!"
    read -p "If you want to reload the file? [Y/N]: "flag
    if [ "$flag" = "Y" ]
    then
        sudo rm $sesfile $logfile
        sudo sh -c "script -t 2>$logfile -a $sesfile"
    else
        echo "Nothing to do !"
    fi
else
    sudo sh -c "script -t 2> $logfile -a $sesfile"
fi