# ModTek

ModTek is a modding system for HBS's BATTLETECH PC game based on [BTML](https://github.com/Mpstark/BattleTechModLoader) that allows modders to package their mods in a self-contained manner without overwritting game files. ModTek is run at game startup (initialized by BTML) and initializies other mods that conform to the [mod.json format](https://github.com/Mpstark/ModTek/wiki/The-mod.json-format). In this way, it allows for the dynamic loading of mods at runtime with dependancies resolved and load order enforced, without having to edit the dreaded `VersionManifest.csv`. It also provides for incrementatal patching of stock game files that are easy to remove, version, and persist through patches.

## Installing

ModTek requires [BTML](https://github.com/Mpstark/BattleTechModLoader).

If the `BATTLETECH\Mods\` directory doesn't exist, create it. Install via moving `ModTek.dll` into the `BATTLETECH\Mods\` folder. BTML will now load ModTek.

On game startup, ModTek decorates the version number found in the bottom left corner of the main menu (introduced in Patch 1.01) with "/W MODTEK". If you don't see this and you're beyond patch 1.01, something has gone wrong.

## Handling Game Updates

Because ModTek is a BTML mod and doesn't change any game files, all you have to do to get your ModTek mods working again after an update is to re-run `BattleTechModLoaderInjector.exe`, which will re-inject BTML into the game.

## Anatomy of a ModTek Mod

```
\BATTLETECH\Mods\
    \MyModDirectory\
        mod.json
        MyDllName.dll

        StreamingAssets\data\weapon\
            Weapon_Autocannon_AC5_0-Stock.json
```

## A Brief Primer on Developing ModTek Mods

It all begins with a `mod.json` file in the root of your mods subdirectory. This is the only non-optional part of ModTek. Contained within is metadata that determines how your mod is loaded, what order it is loaded in, and an optional settings block for configuring your mod (which is only applicable for mods that include a DLL). Further documentation for the `mod.json` format [is here.](https://github.com/Mpstark/ModTek/wiki/The-mod.json-format)

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
        { "Type": "WeaponDef", "Path": "MyCustomWeaponDefsFolder\\" }
        { "Type": "MechDef", "Path": "MyCustomMech\\mechdef_my_super_mech.json" }
    ],

    "Settings": {
        "CoolSetting": 450,
        "GreatSetting": true
    }
}
```

The only required field is "Name" which must be **unique** between all installed mods in a session. The other fields are optional with some having default values, but it is highly recommended that you fill them in for mods intended for distribution. Many of those fields are self-explanatory -- but currently they are only read at game startup. Again, you can read about the `mod.json` format [in-depth here](https://github.com/Mpstark/ModTek/wiki/The-mod.json-format).

If a DLL is supplied with your mod, in order to be loaded and run, it will need to have a path and file name given. Optionally, you can specify an entry point, which defaults to calling all `public static Init(void)` on all classes in your assembly. Some parameters are supported coming into your entry point.

The "Manifest" entry here is of particular note, as this will load files into the `VersionManifest` at load. By default, ModTek assumes that files in `\MyModDirectory\StreamingAssets\` are mirrors of base game files in contained in `\BattleTech_Data\StreamingAssets\` and will load those files without needing to be told about them. There are other implicit directories like `\MyModDirectory\MechDefs`, a list of which can be found in, you guessed it, [the in-depth guide to the `mod.json` format](https://github.com/Mpstark/ModTek/wiki/The-mod.json-format).

## Merging JSON

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

Note: Because of the way ModTek loads mods, **the last mod to change a property "wins"**. Because of this, you should ***absolutely not*** copy the entire file from the stock folder into your mod and make a few changes. Only include the values which you actually want to change.

## License

ModTek, like BTML before it, is provided under the "Unlicence", which releases the work into the public domain.
