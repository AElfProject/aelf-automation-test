#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=./Protobuf/Proto \
--csharp_out=./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
$@