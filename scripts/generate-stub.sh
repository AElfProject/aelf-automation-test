#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

destdir=./Protobuf/Generated

[ -d ${destdir} ] || mkdir -p ${destdir}

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=./Protobuf/Proto \
--csharp_out=${destdir} \
--csharp_opt=file_extension=.g.cs \
--contract_opt=stub \
--contract_out=${destdir} \
--plugin=protoc-gen-contract="${plugin}" \
$@