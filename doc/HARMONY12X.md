# HarmonyX support

BepInEx is a modding tool and a modding community that provides an alternative to the up-to-date Harmony2 called HarmonyX.
To allow support for mods using other versions, BepInEx provides "shims" that redirect calls from older harmony versions to HarmonyX.

ModTek supports loading up HarmonyX with the BepInEx shims for Harmony 1.2 and Harmony 2, see the ModTekPreloader configuration to enable that feature.
This allows mods written against those Harmony versions to coexist with the newest Harmony X.

## Harmony Changes

Potentially breaking changes for Harmony 1 mods that get shimmed:
- The patching backend changed, which is more strict and can also detect issues that were ignored before. That can lead to the patching process throwing errors.
- The shims in use does not cover 100% of the API of the original, e.g. the ILGenerator of Harmony 1.2 is not implemented. In general it works, and even Harmony 1 transpilers are wrapped successfully during the patching process.
- DLLs are not directly loaded from the location where the mod DLL is original at, but it will be re-written to `.modtek/AssembliesShimmed` and loaded from there. The assembly location property is patched to return the original path even if the assembly is loaded from the shimmed directory. This avoids mods reading or writing files from the wrong directory if using paths relative to the location of the mod' DLL.
- Harmony 1 patch sorting was broken and did not work, this was fixed in Harmony 2 and Harmony X, leading to a different executing order of prefixes and postfixes, especially when using priorities and after/before attributes as those were broken too in Harmony 1.

Potentially breaking changes for mods switching from Harmony 1 to Harmony X without using shimming:
- When using Harmony X directly, patch prefixes are not skipped automatically. In Harmony 1, a prefix A that runs before a prefix B could make prefix B skip if A asked to do so. This is not automatically possible anymore when prefix B is directly patched with Harmony X. To keep compatibility with other mods, follow the migration guide on how to make a mods prefix patches skippable for maximum backwards compatibility.

For a full list of changes introduced with Harmony 2, see https://harmony.pardeike.net/articles/new.html .

For a full list of changes introduced with Harmony X, see https://github.com/BepInEx/HarmonyX/wiki/Difference-between-Harmony-and-HarmonyX .

Changes in ModTek:
- Harmony Logging
  - harmony_summary.log was removed, as it was anyway buggy as the order of patches was a guess and one never knew how it was correct, as well as anything patching after the summary was obviously missing from the summary.
  - All patches and the order of patches are now visible in the harmony logs, either at `.modtek/HarmonyFileLog.log` or as part of `.modtek/battletech_log.txt`.
  - Log levels for harmony logging are managed in the ModTekPreloader configuration.
- ModTek uses HarmonyX itself and prefix skipping was not implemented, meaning other mods can't disable or modify ModTek prefixes yet using the normal Harmony 1 mechanisms.
- HarmonyX support exists since ModTek v3, and since ModTek v4 its force enabled and can't be disabled anymore.

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
   
   One can now replace any try/catch with the [HarmonyWrapSafe] attribute, exception thrown by the patch will be logged
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
   [HarmonyWrapSafe]
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
5. One can also add `[HarmonyWrapSafe]` to all postfixes to keep it consistent with the prefixes.
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
   [HarmonyWrapSafe]
   public static void Postfix(MechDef mech, ref HashSet<string> __result)
   {
       // something complicated here
   }
   ```

See the BattleTech, ModTek or Harmony logs in `.modtek` in case you encounter errors.

## History of Modtek and Harmony Versions

BattleTech provides official mod support, though they just copy pasted the ModTek version at the time and named it ModLoader.
Through that they also included the latest official Harmony 1 release as part of BattleTech.

ModTek was sometimes bundled with a slightly customized version of Harmony 1, which only differed in implementation not API.

Harmony 2 was being developed but never integrated into ModTek, it had radical changes in namespace and some APIs and was therefore not backwards compatible, meaning ModTek could not
just replace the Harmony 1 dll with the Harmony 2 dll.

BepInEx is a modding framework and community which implemented a fork of Harmony 2 called Harmony X that changed some things that made it also incompatible in behavior to Harmony 2.
However, since there were several BepInEx mods that were still reliant on Harmony 1, they opted to add shims that allowed mods written against Harmony 1 to be dynamically changed to Harmony X support.

ModTek v3 then started supporting HarmonyX and used the Harmony 1<>X bridging support from BepInEx and improved upon it, since it wasn't properly supporting Harmony 1 behavior.

ModTek v4 is similar to v3, however in v3 one could disable HarmonyX support which would avoid shimming.
With ModTek v4 one can't disable HarmonyX anymore and Harmony 1 mods will always be shimmed.

HarmonyX strives to be backwards compatible with older HarmonyX versions, hence why shimming between HarmonyX versions is not yet necessary.
