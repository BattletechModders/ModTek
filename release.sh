#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"

SEVENZIP="7z"

set -ex

dotnet build --configuration Release --no-incremental -p:OutputPath=../dist/ "$@"

INCLUDES="-i!ModTek/*.dll -i!ModTek/modtekassetbundle -i!ModTek/ModTekInjector.exe -i!ModTek/README.md -i!ModTek/UNLICENSE"

"$SEVENZIP" a -tzip -mx9 dist/ModTek.zip $INCLUDES
