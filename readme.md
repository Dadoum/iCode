# iCode
iCode is a complex heavyweight unreliable iOS IDE for Linux.

## How do I run it ?
iCode requires `mono-runtime`, `gtk-sharp3`, `libgdl-3-5` and `clang` (maybe `gocjc++`) 

These packages can be installed with APT using this command:

`sudo apt install mono-runtime gtk-sharp3 libgdl-3-5 clang gobjc++`

## What can I do with iCode ?
Build simple iOS apps in Objective-C, without storyboard and some features. It generates signed ipas (thanks to [zsign](https://github.com/zhlynn/zsign)) that can be installed on device with ideviceinstaller. Without any mac !
