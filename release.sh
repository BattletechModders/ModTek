#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"
export PATH="/c/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/:$PATH"

SEVENZIP="7z"

set -ex

rm -fr dist/
MSBuild.exe #-property:OutputPath=../dist/
exit 1

#dotnet nuget locals all --clear
#dotnet restore
#dotnet build --configuration Release --no-incremental -p:OutputPath=../dist/ "$@"

#dotnet restore --packages packages/
#dotnet build --packages packages/ --configuration Release --no-incremental -p:OutputPath=../dist/ "$@"

INCLUDES="-i!ModTek/*.dll -i!ModTek/modtekassetbundle -i!ModTek/ModTekInjector.exe -i!ModTek/README.md -i!ModTek/UNLICENSE"

"$SEVENZIP" a -tzip -mx9 dist/ModTek.zip $INCLUDES
