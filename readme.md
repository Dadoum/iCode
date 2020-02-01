# iCode [![Build Status](https://dev.azure.com/DadoumDev/iCode/_apis/build/status/Dadoum.iCode?branchName=master)](https://dev.azure.com/DadoumDev/iCode/_build/latest?definitionId=1&branchName=master)
iCode is a work-in-progress IDE to build simple iOS

**Actually, the script that generates certificates is broken. I investigate Apple's server API to fix this issue. In the meantime, you can generate certificates elsewhere and put it in the developer folder like the ReadMe file says.**

## How do I run it ?
iCode does requires `libclang`*

This package can be installed with APT using this command:

`sudo apt install libclang`

## What can I do with iCode ?
Build simple iOS apps in Objective-C, without storyboard and some features. It generates signed ipas (thanks to [zsign](https://github.com/zhlynn/zsign)) that can be installed on device with ideviceinstaller. Without any mac !
Also, you can use some utils like `ibtool` to use these functions

*iCode may crash on project creation; if it is the case, install `xcb` package to fix that. 

In addition, in some distribution, `libclang` is not bundled with `clang`; if so, install it.
