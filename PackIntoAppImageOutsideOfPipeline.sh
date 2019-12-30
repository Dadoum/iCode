#!/bin/bash
rm -rf ./AppDir
rm appimagetool-x86_64.AppImage
dotnet publish --configuration Debug -r linux-x64 --self-contained true

./PackIntoAppImage.sh
