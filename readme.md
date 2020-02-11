# iCode [![Build Status](https://dev.azure.com/DadoumDev/iCode/_apis/build/status/Dadoum.iCode?branchName=master)](https://dev.azure.com/DadoumDev/iCode/_build/latest?definitionId=1&branchName=master)
iCode is a work-in-progress IDE to build simple iOS

You just need to bring your certificates and to place it in the correct folder !

## How to run iCode
iCode does requires `clang`.

This package can be installed with APT using this command:

`sudo apt install libclang`

Or from Arch Linux:

`sudo pacman -S clang`

## The purpose of iCode
Build simple iOS apps in Objective-C, without storyboard and some features. It generates signed ipas (thanks to [zsign](https://github.com/zhlynn/zsign)) that can be installed on device with ideviceinstaller. Without any mac !
Also, you can use some utils like `ibtool` to use these functions
