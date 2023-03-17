# HarmonyX support

BepInEx is a modding tool and a modding community that provides an alternative to the up-to-date Harmony2 called HarmonyX.
To allow support for mods using other versions, BepInEx provides "shims" that redirect calls from older harmony versions to HarmonyX.

ModTek supports loading up HarmonyX with the BepInEx shims for Harmony 1.2 and Harmony 2, see the ModTekPreloader configuration to enable that feature.
This allows mods written against those Harmony versions to coexist.

## Migration Guide

If your mod uses Harmony 1 and you want to migrate to HarmonyX instead of letting ModTek shim during runtime, one can follow the steps outlined below:

Preparation:
1. Add a global using file called `GlobalUsings.cs` and add `global using Harmony;` to it
2. Remove `using Harmony;` from any source files, best use Resharper and Rider to automatically find and remove un-used using statements.

Actual migration:
1. In the mods `csproj` file, replace the 0Harmony (Harmony 1) Reference with a HarmonyX PackageReference.
    ```csharp
    <Reference Include="0Harmony">
      <Private>False</Private>
    </Reference>
    ```
    HarmonyX:
    ```csharp
    <PackageReference Include="HarmonyX" Version="2.10.1">
      <PrivateAssets>all</PrivateAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>
    ```
2. Change the global using introduced in the preparation to `global using HarmonyLib;`
3. Update the Harmony patching mechanism, as it changed.
   ```csharp
   var harmony = HarmonyInstance.Create("my harmony identifier");
   harmony.PatchAll(Assembly.GetExecutingAssembly());
   ```
   HarmonyX:
   ```csharp
   Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "my harmony identifier");
   ```
4. In every patch prefix, add a __runOriginal check and return if a previous patch wants to skip the original method.
   That emulates the behavior from Harmony1 and is necessary so other mods can make your prefix not execute.
   Prefixes not skipping automatically anymore is the main difference between Harmony from pardeike and HarmonyX from BepInEx.
   
   One can now replace any try/catch with the [HarmonySafeWrap] attribute, exception thrown by the patch will be logged
   to the HBS Logger "HarmonyX" and thus will be available in the logs under `.modtek`.
   ```csharp
   [HarmonyPrefix]
   public static bool Prefix(MechDef mech, ref HashSet<string> __result)
   {
       if (mech == null)
       {
           return true;
       }
     
       try
       {
           // something complicated here
   
           return false;
       }
       catch (Exception e)
       {
           Log.Main.Error?.Log("This should not have happened", e);
           return true;
       }
   }
   ```
   HarmonyX:
   ```csharp
   [HarmonyPrefix]
   [HarmonySafeWrap]
   public static void Prefix(ref bool __runOriginal, MechDef mech, ref HashSet<string> __result)
   {
       if (!__runOriginal)
       {
           return;
       }
   
       if (mech == null)
       {
           return;
       }
     
       // something complicated here
   
       __runOriginal = false;
   }
   ```
5. One can also add `[HarmonySafeWrap]` to all postfixes to keep it consistent with the prefixes.
   ```csharp
   [HarmonyPostfix]
   public static bool Postfix(MechDef mech, ref HashSet<string> __result)
   {
       try
       {
           // something complicated here
       }
       catch
       {
           // maybe some logging is being done here
           // or we just don't want to crash the whole game when our mod misbehaves
       }
   }
   ```
   HarmonyX:
   ```csharp
   [HarmonyPostfix]
   [HarmonySafeWrap]
   public static void Postfix(MechDef mech, ref HashSet<string> __result)
   {
       // something complicated here
   }
   ```

See the BattleTech, ModTek or Harmony logs in `.modtek` in case you encounter errors.

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

With BepInEx 6 they will not support older harmony versions anymore, however shimming is still working even with newer HarmonyX.
Also BepInEx 5 is still supported in LTS mode and does support the harmony shims.

## How to use it

Reference `Mods/ModTek/Harmony12X/0Harmony.dll` to use the latest HarmonyX version and enable HarmonyX in the `ModTek/config.json`.
