#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"

SEVENZIP="7z"

set -ex

rm -fr dist/

dotnet build ModTek --configuration Release --no-incremental -p:OutputPath=../dist/ModTek "$@"
dotnet build ModTekInjector --configuration Release --no-incremental -p:OutputPath=../dist/ModTek "$@"

cp -a README.md dist/ModTek
cp -a UNLICENSE dist/ModTek

"$SEVENZIP" a -tzip -mx9 dist/ModTek.zip -ir!./dist/ModTek
