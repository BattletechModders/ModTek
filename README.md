# ModTek

ModTek is a modding system for HBS's BATTLETECH PC game based on [BTML](https://github.com/Mpstark/BattleTechModLoader) that allows modders to package their mods in a self-contained manner without overwritting game files. ModTek is run at game startup (initialized by BTML) and initializies other mods that conform to the [ModTek .json Format](). In this way, it allows for the dynamic loading of mods at runtime with dependancies resolved and load order enforced, without having to edit the dreaded `VersionManifest.csv`. It also provides for incrementatal patching of stack game files that are easy to remove, version, and persist through patches.

***THERE ARE NO RELEASES YET -- IN HEAVY DEVELOPMENT!***

## Installing

ModTek requires [BTML](https://github.com/Mpstark/BattleTechModLoader). Install BTML according to its instructions, and then move ModTek.dll into your `\BATTLETECH\Mods\` directory. On game startup, ModTek decorates the version number found in the bottom left corner of the main menu (introduced in Patch 1.01) with "/W MODTEK". If you don't see this and you're beyond patch 1.01, something has gone wrong.

## Anatomy of a ModTek Mod

```
\BATTLETECH\Mods\
    \MyModDirectory\
        MyModName.modtek.json
        MyDllName.dll

        \(WeaponDef)\
            Weapon_Autocannon_AC5_0-Stock.json
```

## A Brief Primer on Developing ModTek Mods

It all begins with a `MyModName.modtek.json` file in the root of your mods subdirectory. This is the only non-optional part of ModTek. Contained within is metadata that determines how your mod is loaded, what order it is loaded in, and an optional settings block for configuring your mod (which is only applicable for mods that include a DLL). Further documentation for the `.modtek.json` format [is here.]() 

Here's an example `.modtek.json`:

```JSON
{
    "Name": "ReallyCoolMod",
    "Version": "0.0.1",
    "Enabled": true,

    "DependsOn": [ "ReallyCoolOtherMod", "RadicalMod" ],
    "ConflictsWith": [ "IncompatableMod" ],

    "DLL": "LegomanCoolMod.dll",
    "DLLEntryPoint": "CoolModNamespace.CoolModClass.CoolModPublicStaticMethod",

    "Manifest": [
        { "Type": "WeaponDef", "Path": "\\MyCustomWeaponDefsFolder\\" }
        { "Type": "MechDef", "Path": "\\MyCustomMech\\mechdef_my_super_mech.json" }
    ],

    "Settings": {
        "CoolSetting": 450,
        "GreatSetting": true
    }
}
```

The only required field is "Name" which must be **unique** between all installed mods in a session. The other fields are optional with some having default values, but it is highly recommended that you fill them in for mods intended for distribution. Many of those fields are self-explanatory -- but currently they are only read at game startup. Again, you can read about the `.modtek.json` format [in-depth here]().

If a DLL is supplied with your mod, in order to be loaded and run, it will need to have a path and file name given. Optionally, you can specify an entry point, which defaults to calling all `public static Init(void)` on all classes in your assembly. Some parameters are supported coming into your entry point.

The "Manifest" entry here is of particular note, as this will load files into the `VersionManifest` at load. By default, ModTek assumes that files in `\MyModDirectory\data\` are mirrors of base game files in contained in `\BattleTech_Data\StreamingAssets\data` and will load those files without needing to be told about them. There are other implicit directories like `\MyModDirectory\MechDefs`, a list of which can be found in, you guessed it, [the in-depth guide to the `.modtek.json` format]().

## Merging JSON

For JSON files of specific types, if a file is loaded that has the same ID as a file that is already in the game, instead of completely replacing the file, ModTek will do a simple merge of the two types when these files are deserialized from JSON. Here's a simple example of a mod that changes the AC/5s damage to give it a *little* boost.

`BoostedAC5.modtek.json`:

```JSON
{
    "Name": "BoostedAC5",
    "Version": "0.0.1"
}
```

`\BoostedAC5\data\weapon\Weapon_Autocannon_AC5_0-Stock.json`:

```JSON
{
    "Damage": 450
}
```

This change would make the AC/5 do 450 damage and all other settings on the stock AC/5 will be unchanged.

Note: Because of the way ModTek loads mods, **the last mod to change a property "wins"**. Because of this, you should ***absolutely not*** copy the entire file from the stock folder into your mod and make a few changes. Only include the values which you actually want to change.

## License

ModTek, like BTML before it, is provided under the "Unlicence", which releases the work into the public domain.
