#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"

SEVENZIP="7z"

set -ex

rm -fr dist/

dotnet build ModTek --configuration Release --no-incremental -p:OutputPath=../dist/ "$@"
dotnet build ModTekInjector --configuration Release --no-incremental -p:OutputPath=../dist/ "$@"

INCLUDES="-i!./dist/* -i!README.md -i!UNLICENSE"

"$SEVENZIP" a -tzip -mx9 dist/ModTek.zip $INCLUDES
