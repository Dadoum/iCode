#!/bin/bash
rm -rf ./AppDir
rm -rf ./bin/Debug/netcoreapp3.0/linux-x64/
dotnet publish --configuration Debug -r linux-x64 --self-contained true 
./PackIntoAppImage.sh
