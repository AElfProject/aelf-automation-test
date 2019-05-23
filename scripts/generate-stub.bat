SET scriptdir=%~dp0

call "%scriptdir%download_binary.bat"

protoc --proto_path=../AElf/protobuf ^
--csharp_out=internal_access:./Protobuf\Generated ^
--csharp_opt=file_extension=.g.cs ^
--contract_opt=stub ^
--contract_out=./Protobuf/Generated ^
--plugin=protoc-gen-contract="%scriptdir%contract_csharp_plugin.exe" ^
%*