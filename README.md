# ModTek

ModTek is a modding system for HBS's BATTLETECH PC game based on [BTML](https://github.com/janxious/BattleTechModLoader) that allows modders to package their mods in a self-contained manner without overwritting game files. ModTek is run at game startup (initialized by BTML) and initializies other mods that conform to the [mod.json format](https://github.com/Mpstark/ModTek/wiki/The-mod.json-format). In this way, it allows for the dynamic loading of mods at runtime with dependancies resolved and load order enforced, without having to edit the dreaded `VersionManifest.csv`. It also provides for incrementatal patching of stock game files that are easy to remove, version, and persist through patches.

## Clarification

This is a fork of Mpstark's great work here: https://github.com/Mpstark/ModTek. Many of the links point to wiki articles from that repo but the releases in this repo are the ones to use.

## Installing

[A step-by-step install guide for BTML + ModTek + ModTek mods.](https://github.com/janxious/ModTek/wiki/The-Drop-Dead-Simple-Guide-to-Installing-BTML-&-ModTek-&-ModTek-mods)

ModTek requires [BTML](https://github.com/janxious/BattleTechModLoader).

If the `BATTLETECH\Mods\` directory doesn't exist, create it. Install via moving `ModTek.dll` into the `BATTLETECH\Mods\` folder. BTML will now load ModTek.

On game startup, ModTek decorates the version number found in the bottom left corner of the main menu (introduced in Patch 1.01) with "/W MODTEK". If you don't see this and you're beyond patch 1.01, something has gone wrong.

## Handling Game Updates

Because ModTek is a BTML mod and doesn't change any game files, all you have to do to get your ModTek mods working again after an update is to re-run `BattleTechModLoaderInjector.exe`, which will re-inject BTML into the game. If the game has changed in a way that breaks an individual mod, that mod will need to be updated, but generally, this will only happen in large updates.

## Anatomy of a ModTek Mod

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

It all begins with a `mod.json` file in the root of your mods subdirectory. This is the only non-optional part of ModTek. Contained within is metadata that determines how your mod is loaded, what order it is loaded in, and an optional settings block for configuring your mod (which is only applicable for mods that include a DLL). Further documentation for the `mod.json` format [is here.](https://github.com/janxious/ModTek/wiki/The-mod.json-format)

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

The only required field is "Name" which must be **unique** between all installed mods in a session. The other fields are optional with some having default values, but it is highly recommended that you fill them in for mods intended for distribution. Many of those fields are self-explanatory -- but currently they are only read at game startup. Again, you can read about the `mod.json` format [in-depth here](https://github.com/janxious/ModTek/wiki/The-mod.json-format).

If a DLL is supplied with your mod, in order to be loaded and run, it will need to have a path and file name given. Optionally, you can specify an entry point, which defaults to calling all `public static Init(void)` on all classes in your assembly. Some parameters are supported coming into your entry point.

The "Manifest" entry here is of particular note, as this will load files into the `VersionManifest` at load. By default, ModTek assumes that files in `\MyModDirectory\StreamingAssets\` are mirrors of base game files in contained in `\BattleTech_Data\StreamingAssets\` and will load those files without needing to be told about them.

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

Note: Because of the way ModTek loads mods, **the last mod to change a property "wins"**. Because of this, you should probably ***not*** copy the entire file from the stock folder into your mod and make a few changes. Only include the values which you actually want to change.

## Advanced JSON Merging

Using the standard merging mechanisms, one can't merge arrays or remove data.
Advanced JSON Merging adds several ways to surgically manipulate existing JSONs using [JSONPath](https://goessner.net/articles/JsonPath/).

To use Advanced JSON Merging, add a manifest to your `mod.json` and add an "AdvancedJSONMerge" type entry to it. All jsons found under Path will be assumed to be AdvancedJSONMerge instruction files.

Example Manifest with Advanced JSON Merging entry.
```JSON
"Manifest": [
	{
		"Type": "AdvancedJSONMerge",
		"Path": "advanced",
		"ShouldMergeJSON": false
	}
]
```

Example Advanced JSON Merging instructions json `advanced/blackknight_changes.json`

Removes all heat sinks from the mech and then adds in back two heat sinks in the center torso.
```JSON
{
	"TargetFile": "data/mech/mechdef_blackknight_BL-6-KNT.json",
	"Instructions": [
		{
			"JSONPath": "inventory[?(@.ComponentDefID == 'Gear_HeatSink_Generic_Standard')]",
			"Action": "Remove"
		},
		{
			"JSONPath": "inventory",
			"Action": "ArrayConcat",
			"Value": [
				{
					"MountedLocation": "CenterTorso",
					"ComponentDefID": "Gear_HeatSink_Generic_Standard",
					"ComponentDefType": "Upgrade",
					"DamageLevel": "Functional"
				},
				{
					"MountedLocation": "CenterTorso",
					"ComponentDefID": "Gear_HeatSink_Generic_Standard",
					"ComponentDefType": "Upgrade",
					"DamageLevel": "Functional"
				}
			]
		}
	]
}
```

### JSONPath Examples

[JSONPath](https://goessner.net/articles/JsonPath/) is a good documented standard for navigating jsons.
One can also find lots of solutions to problems on [stackoverflow](https://stackoverflow.com/questions/tagged/jsonpath).

### Action Examples

The sources of ModTek contain unit tests with some examples on how to use Actions.

`ArrayAdd` adds a given value to the end of the target array.
```JSON
{
	"JSONPath": "inventory",
	"Action": "ArrayAdd",
	"Value": {
		"MountedLocation": "CenterTorso",
		"ComponentDefID": "Gear_HeatSink_Generic_Standard",
		"ComponentDefType": "Upgrade",
		"DamageLevel": "Functional"
	}
}
```

`ArrayAddAfter` adds a given value after the target element in the array.
```JSON
{
	"JSONPath": "inventory[0]",
	"Action": "ArrayAddAfter",
	"Value": {
		"MountedLocation": "CenterTorso",
		"ComponentDefID": "Gear_HeatSink_Generic_Standard",
		"ComponentDefType": "Upgrade",
		"DamageLevel": "Functional"
	}
}
```

`ArrayAddBefore` adds a given value before the target element in the array.
`inventory[-1:]` references the last element of the inventory array.
Example adds a component to the second last position of the inventory.
```JSON
{
	"JSONPath": "inventory[-1:]",
	"Action": "ArrayAddBefore",
	"Value": {
		"MountedLocation": "CenterTorso",
		"ComponentDefID": "Gear_HeatSink_Generic_Standard",
		"ComponentDefType": "Upgrade",
		"DamageLevel": "Functional"
	}
}
```

`ArrayConcat` adds a given array to the end of the target array.
Allows to add multiple elements quickly without having to "ArrayAdd" them individually.
```JSON
{
	"JSONPath": "inventory",
	"Action": "ArrayConcat",
	"Value": [
		{
			"MountedLocation": "CenterTorso",
			"ComponentDefID": "Gear_HeatSink_Generic_Standard",
			"ComponentDefType": "Upgrade",
			"DamageLevel": "Functional"
		},
		{
			"MountedLocation": "CenterTorso",
			"ComponentDefID": "Gear_HeatSink_Generic_Standard",
			"ComponentDefType": "Upgrade",
			"DamageLevel": "Functional"
		}
	]
}
```

`ObjectMerge` merges a given object with the target objects.
Example selects the head location and sets new armor values.
```JSON
{
	"JSONPath": "Locations[?(@.Location == 'Head')]",
	"Action": "ObjectMerge",
	"Value": {
		"CurrentArmor": 100,
		"AssignedArmor": 100
	}
}
```

`Remove` removes the target element(s).
Example removes all components from inventory that are heat sinks.
```JSON
{
	"JSONPath": "inventory[?(@.ComponentDefID == 'Gear_HeatSink_Generic_Standard')]",
	"Action": "Remove"
}
```

`Replace` replaces the target with a given value.
Example replaces the mech tags with a new list of tags.
```JSON
{
	"JSONPath": "MechTags/items",
	"Action": "Replace",
	"Value": [
		"unit_mech",
		"unit_heavy",
		"unit_role_brawler"
	]
}
```

## Building It
In the project folder there is an example project user file (e.g. `ModTek.csproj.user.example`). You can make a copy of that file and rename it without the `.example` ending and then update it to point to your BTG Managed DLL folder.

## License

ModTek is provided under the [Unlicense](UNLICENSE), which releases the work into the public domain.
