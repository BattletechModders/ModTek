#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"

SEVENZIP="7z"

set -ex

rm -fr dist/

dotnet build ModTek -c Release -p:OutputPath=./dist/ModTek "$@"
dotnet publish ModTekInjector -c Release -o ./dist/ModTek "$@"

cp -a README.md dist/ModTek
cp -a UNLICENSE dist/ModTek

"$SEVENZIP" a -tzip -mx9 dist/ModTek.zip -ir!./dist/ModTek
