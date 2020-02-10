mkdir -p ./AppDir/usr/bin
cp -r ./bin/Debug/netcoreapp3.0/linux-x64/publish/* ./AppDir/usr/bin/
./linuxdeploy-x86_64.AppImage --appdir=./AppDir/ -e./bin/Debug/netcoreapp3.0/linux-x64/publish/iCode -i./resources/images/icon/scalable.svg -i./resources/images/icon/16.png -i./resources/images/icon/32.png -i./resources/images/icon/64.png -i./resources/images/icon/128.png -i./resources/images/icon/256.png -i./resources/images/icon/512.png --desktop-file=./iCode.desktop 
cp ./natives/* ./AppDir/usr/bin/
./appimagetool-x86_64.AppImage ./AppDir -u "gh-releases-zsync|Dadoum|iCode|latest|iCode-*x86_64.AppImage.zsync"
chmod +x iCode-x86_64.AppImage
