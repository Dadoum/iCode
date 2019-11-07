# iCode [![Build Status](https://dev.azure.com/DadoumDev/iCode/_apis/build/status/Dadoum.iCode?branchName=master)](https://dev.azure.com/DadoumDev/iCode/_build/latest?definitionId=1&branchName=master)
iCode is a complex heavyweight unreliable iOS IDE for Linux.

## How do I run it ?
iCode requires `mono-runtime`, `gtk-sharp3`, `libgdl-3-5`, `xcb` and `clang` (maybe `gocjc++`) *

These packages can be installed with APT using this command:

`sudo apt install mono-runtime gtk-sharp3 libgdl-3-5 clang gobjc++`

## What can I do with iCode ?
Build simple iOS apps in Objective-C, without storyboard and some features. It generates signed ipas (thanks to [zsign](https://github.com/zhlynn/zsign)) that can be installed on device with ideviceinstaller. Without any mac !

*Utility of each package:
 - `mono-runtime` permits to run C# applications in Linux. iCode is made in C#.
 - `gtk-sharp3` is a C# binding for GTK. It manages all the UI parts.
 - `libgdl-3-5` is a docking library for GTK. It permits to its binding gdl-sharp to run.
 - `xcb` iCode crashes on project creation without it.  
 - `clang` is the compiler. 
 - `gobjc++` is not required but I'm not sure so if you have enough space on your disk it is always good to have.
