# HarmonyX support

BepInEx is a modding tool and a modding community that provides an alternative to the up-to-date Harmony2 called HarmonyX.
To allow support for mods using other versions, they implemented "shims" that redirect calls from older harmony versions to HarmonyX.

ModTek supports loading up HarmonyX with the BepInEx shims for Harmony 1.2 and Harmony 2, see the ModTekPreloader configuration to enable that feature.
This allows mods written against those Harmony versions to coexist.

## How it works internally

Note that in order for it to work, the ModTekPreloader has to intercept assembly load calls and rewrite the assembly. They are then saved to disk
at a location under `.modtek`. Due to how harmony is being distributed, all breaking versions of harmony have the same assembly name, and .NET can't load
different assemblies under the same name. The shim is made to work by renaming the dependencies in assemblies, e.g. a ModTek.dll that references
the `0Harmony.dll` version 1.2 will have the reference changed to the shim of the name `OHarmony12.dll`.

## Limitations

The shims do not cover 100% of the API of the original versions, e.g. the ILGenerator of Harmony 1.2 is not implemented.
However almost no mods use that. In general, transpilers are wrapped successfully via shims and do work. Sometimes a buggy
implementation in Harmony 1 allowed for invalid or broken patching, which would now throw an error during patching due to 
HarmonyX being more strict and bug free.

With BepInEx 6 they will not support older or alternative harmony versions anymore, so we are stuck to the current versions of harmony.
Fortunately BepInEx 5 is still supported in LTS mode and does support the harmony shims.

ModTek continues to use Harmony 1.2, which means that it itself requires the BepInEx 5 Harmony shims.

## How to use it

Reference `Mods/ModTek/Harmony12X/0Harmony.dll` to use the latest HarmonyX version that works with ModTek.
