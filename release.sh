#!/bin/bash

export PATH="/c/Program Files/7-Zip/:$PATH"

SEVENZIP="7z"

set -ex

DIST="$(pwd)/dist/ModTek"
rm -fr "${DIST}"

dotnet --version
dotnet publish ModTekInjector -c Release -o "${DIST}"
dotnet build ModTek -c Release -p:OutputPath="${DIST}" "$@"

cp -a README.md "${DIST}"
cp -a UNLICENSE "${DIST}"

"$SEVENZIP" a -tzip -mx9 "${DIST}".zip -ir!"${DIST}"
