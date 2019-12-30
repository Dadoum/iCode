#!/bin/bash
rm -rf ./AppDir
rm appimagetool-x86_64.AppImage
dotnet publish --configuration Debug -r linux-x64 --self-contained true

mkdir -p ./AppDir/usr/bin
cp -r ./bin/Debug/netcoreapp3.0/linux-x64/* ./AppDir/usr/bin/
touch ./AppDir/AppRun
echo "#!/bin/sh" >> ./AppDir/AppRun
echo "HERE=\"\$(dirname \"\$(readlink -f \"\${0}\")\")\"" >> ./AppDir/AppRun
echo "export PATH=\"\${HERE}\"/usr/bin/:\"\${PATH}\"" >> ./AppDir/AppRun
echo "EXEC=\$(grep -e '^Exec=.*' \"\${HERE}\"/*.desktop | head -n 1 | cut -d \"=\" -f 2 | cut -d \" \" -f 1)" >> ./AppDir/AppRun
echo "exec \"\${EXEC}\" \$@" >> ./AppDir/AppRun
chmod 755 ./AppDir/AppRun
chmod +x ./AppDir/AppRun
touch ./AppDir/iCode.desktop
echo "# Desktop Entry Specification: https://standards.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html" >> ./AppDir/iCode.desktop
echo "[Desktop Entry]" >> ./AppDir/iCode.desktop
echo "Type=Application" >> ./AppDir/iCode.desktop
echo "Name=iCode" >> ./AppDir/iCode.desktop
echo "Comment=iOS Development Environement" >> ./AppDir/iCode.desktop
echo "Icon=iCode" >> ./AppDir/iCode.desktop
echo "Exec=iCode" >> ./AppDir/iCode.desktop
echo "Path=~" >> ./AppDir/iCode.desktop
echo "Terminal=true" >> ./AppDir/iCode.desktop
echo "Categories=Development;" >> ./AppDir/iCode.desktop
cp ./resources/images/icon.png ./AppDir/iCode.png
wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage ./AppDir -u "gh-releases-zsync|Dadoum|iCode|latest|iCode-*x86_64.AppImage.zsync"
chmod +x iCode-x86_64.AppImage
