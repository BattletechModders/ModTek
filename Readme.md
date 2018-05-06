# ModTek

ModTek is a mod which allows the dynamic loading of other modifications to the base BATTLETECH game by HBS. In addition to ensuring that 
load order is respected, ModTek also allows incremental patching of the stock game files in a way that is easy to remove, version, and 
persists through patches.

## Installing ModTek

ModTek requires the use of the [BTML](https://github.com/Mpstark/BattleTechModLoader). Install the BTML, and then install ModTek.dll to your
BATTLETECH/Mods directory.

## Developing a mod which uses ModTek

Every ModTek-compatible mod consists of the following structure:

```
  Author-ModName\
  .. mod.json
  .. (Author-ModName).dll
  .. (WeaponDef)\
  .... Weapon_Autocannon_AC5_0-Stock.json
```

Of the structure noted, only mod.json is required. Some mods will not have DLLs, nor will other have definition folders.

A basic mod.json looks like this:

```
{
    "Name": "Legoman-ReallyCoolMod",
    "Enabled": true,
    "Version": "0.0.1",
    "DependsOn": [],
    "LoadBefore": [],
    "LoadAfter": [],
    "ConflictsWith": [],

    "Manifest": [
        {
            "Type": "WeaponDef",
            "Path": "WeaponDefs"
        }
    ],

	"DLL": "LegomanCoolMod.dll"
	"DLLEntryPoint": "CoolModClass",

	"Settings": {
	  "LegomanAc5DamageValue": 450
	}
}
```

* The Name is required, and must be unique. For this reason, it is recommended to use the format of `{AuthorName}-{ModName}`.
* The Enabled field must be set to true to function. Toggling this to false is useful for debugging purposes.
* The Version is required, and should be some format which makes sense. The recommendation is that prerelease versions always start with '0', and release versions take the form 'YYYY.MM.DD.releasenumber'.
* Mods listed in DependsOn will be loaded before this mod. In the event that those mods are not installed, **ModTek will not load your mod**
* Mods listed in LoadBefore will be loaded before this mod, but are not required to function.
* Mods listed in LoadAfter will be loaded after this mod, but are not required for this mod to function.
* If you mod would be loaded, but a mod in "ConflictsWith" has already been loaded, then **ModTek will not load your mod**. Use this field sparingly.
* The manifest is a simple directory of files which should be parsed by ModTek. Files referenced in the "Path" portion of these stanzas will be loaded and appended to the internal version manifest the game will load.
* DLL specifies a DLL which ModTek should load as well. This is optional, but 
* DLL Entry Point specifies a class where ModTek should look for a `public static void Init()` method to call. This method is your cue to inject Harmony behaviors.
* Settings is used from within your DLL to fetch dynamic values without requiring a recompile. More on that below.

## Patching JSON with ModTek

Assume the structure we have listed earlier - where we have the `Weapon_Autocannon_AC5_0-Stock.json` file. Because this file directly shadows a file already loaded by the game, ModTek will *merge* your file
with the game's file dynamically. For example, if you wanted AC/5s to get a little bit of a boost, the file could just be the following:

```
  { "Damage": 450 }
```

This change would make the AC/5 do 450 damage, and probably ensure the rapid uninstallation of your mod. All other settings on the stock AC/5 will be unchanged.

Because of the way ModTek loads mods, **the last mod to change a property "wins"**. Because of this, you should ***absolutely not*** copy the entire file from the
stock folder into your mod and make a few changes. Only include the values which you actually want to change.

If you would like to add a brand new file declaration to BATTLETECH, such as a new weapon or equipment, then you may do so by including JSON files here.