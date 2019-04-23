#! /bin/bash

read -p "Please input the session filename: " filename
logfile="$filename.timing.log"
sesfile="$filename.session"
if [ -e $sesfile ]
then
    sudo scriptreplay $logfile $sesfile
    echo
else
    echo "$filename is Not Exist!"
fi