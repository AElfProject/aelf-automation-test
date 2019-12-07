SET scriptdir=%~dp0

call "%scriptdir%download_binary.bat"

protoc --proto_path=./Protobuf/Proto ^
--csharp_out=./Protobuf/Generated ^
--csharp_opt=file_extension=.g.cs ^
%*