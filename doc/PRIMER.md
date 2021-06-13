# Anatomy of a ModTek Mod

```
\BATTLETECH\Mods\
    \ReallyCoolMod\
        mod.json
        LegomanCoolMod.dll

        MyCustomWeaponDefsFolder\
            Weapon_UAC5.json
            Weapon_LB10_X.json

        MyCustomMech\
            mechdef_my_super_mech.json

        StreamingAssets\data\weapon\
            Weapon_Autocannon_AC5_0-Stock.json
```

## A Brief Primer on Developing ModTek Mods

It all begins with a `mod.json` file in the root of your mods subdirectory. This is the only non-optional part of ModTek. Contained within is metadata that determines how your mod is loaded, what order it is loaded in, and an optional settings block for configuring your mod (which is only applicable for mods that include a DLL). Further documentation for the `mod.json` format [is here.](https://github.com/BattletechModders/ModTek/wiki/The-mod.json-format)

Here's an example `mod.json`:

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
        { "Type": "WeaponDef", "Path": "MyCustomWeaponDefsFolder\\" },
        { "Type": "MechDef", "Path": "MyCustomMech\\mechdef_my_super_mech.json" }
    ],

    "Settings": {
        "CoolSetting": 450,
        "GreatSetting": true
    }
}
```

The only required field is "Name" which must be **unique** between all installed mods in a session. The other fields are optional with some having default values, but it is highly recommended that you fill them in for mods intended for distribution. Many of those fields are self-explanatory -- but currently they are only read at game startup. Again, you can read about the `mod.json` format [in-depth here](https://github.com/BattletechModders/ModTek/wiki/The-mod.json-format).

The "Manifest" entry here is of particular note, as this will load files into the `VersionManifest` at load. By default, ModTek assumes that files in `\MyModDirectory\StreamingAssets\` are mirrors of base game files in contained in `\BattleTech_Data\StreamingAssets\` and will load those files without needing to be told about them.

# DLL Mods

You can supply a DLL with your mod by specifying the path and file as the `DLL` property in your mod.json. When ModTek initializes your mod, it will invoke an entry points. By default all methods of type `public static void Init()` in your assembly will be invoked. No ordering is guaranteed for these calls. You are __strongly__ encouraged to specify the exact method name for your entry point via the `DLLEntryPoint` property. Doing so makes the ModTek load process faster as it does not need to scan your assembly for all methods.

Init() calls will be passed two string parameters. The first value will be the modDirectory as an absolute file system path. The second value is the contents of the parsed mod.json. 

In addition to the `Init()` call, ModTek will invoke a different method once all mods are loaded and JSON is merged. Any `public static FinishedLoading()` method in your assembly will be invoked at this time. 

Any mod that throws an error during `Init` will be skipped on the current load. It will be retried on future runs.

# Merging JSON

For JSON files of specific types, if a file is loaded that has the same ID as a file that is already in the game, instead of completely replacing the file, ModTek will do a simple merge of the two types when these files are deserialized from JSON. Here's a simple example of a mod that changes the AC/5s damage to give it a *little* boost.

`mod.json`:

```JSON
{
    "Name": "BoostedAC5",
    "Version": "0.0.1"
}
```

`\BoostedAC5\StreamingAssets\data\weapon\Weapon_Autocannon_AC5_0-Stock.json`:

```JSON
{
    "Damage": 450
}
```

This change would make the AC/5 do 450 damage and all other settings on the stock AC/5 will be unchanged.

Note: Because of the way ModTek loads mods, **the last mod to change a property "wins"**. Because of this, you should probably ***not*** copy the entire file from the stock folder into your mod and make a few changes. Only include the values which you actually want to change.
