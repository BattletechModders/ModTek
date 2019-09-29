#!/bin/bash

set -ex

cd ..

SEVENZIP="/c/Program Files/7-Zip/7z"

INCLUDES="-i!ModTek/*.dll -i!ModTek/modtekassetbundle -i!ModTek/ModTekInjector.exe -i!ModTek/README.md -i!ModTek/UNLICENSE"

"$SEVENZIP" a -tzip -mx9 ModTek/ModTek.zip $EXCLUDES_ALL $INCLUDES
