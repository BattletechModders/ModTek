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

### ContentPackMerges

If you want to merge changes to base game content, you can use `StreamingAssets`:
> mods/{MOD}/StreamingAssets/{ID}.{FILE_EXTENSION}

Example:
> mods/YourMod/StreamingAssets/chassisdef_atlas_AS7-D-HT.json

For merging DLC content, you can now use this directory structure:
> mods/{MOD}/ContentPackMerges/{CONTENT_PACK_ID}/{RESOURCE_TYPE}/{ID}.{FILE_TYPE}

Example:
> mods/YourMod/ContentPackMerges/heavymetal/ChassisDef/chassisdef_annihilator_ANH-JH.json

Variables:
- {MOD}: The name of your mod.
- {ID}: The identifier of the resource, is the same as the filename without the file extension.
- {FILE_EXTENSION}: .json .csv and .txt are valid values.
- {CONTENT_PACK_ID}: Any of shadowhawkdlc, flashpoint, urbanwarfare and heavymetal.
- {RESOURCE_TYPE}: See the generated Manifest.csv file under Mods/.modtek for types and ids. To see the original content, visit [design-data](https://github.com/caardappel-hbs/bt-dlc-designdata) .

## Advanced JSON Merging

Using the standard merging mechanisms, one can't merge arrays or remove data.
Advanced JSON Merging adds several ways to surgically manipulate existing JSONs using [JSONPath](https://goessner.net/articles/JsonPath/).

To use Advanced JSON Merging, add a manifest to your `mod.json` and add an "AdvancedJSONMerge" type entry to it. All jsons found under Path will be assumed to be AdvancedJSONMerge instruction files.

Example Manifest with Advanced JSON Merging entry.

```JSON
{
    "Manifest": [
        {
            "Type": "AdvancedJSONMerge",
            "Path": "advanced"
        }
    ]
}
```

Example Advanced JSON Merging instructions json `advanced/blackknight_changes.json`

Removes all heat sinks from the mech and then adds in back two heat sinks in the center torso.

```JSON
{
    "TargetID": "mechdef_blackknight_BL-6-KNT",
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

# Custom Types

ModTek supports several types that are not handled by the base game. Each of these are described below, and may have unique mechanics. 

## Custom Debug Settings

HBS defined various configuration options in the `BATTLETECH\BattleTech_Data\StreamingAssets\data\debug\settings.json` file. Internal logger levels are defined here, as are some configuration settings only intended to be used by developers. Mods can modify these values using the `DebugSettings` custom type. The contents of any such files will be merged into the HBS provided settings.json. 

This custom type is last-in, last-out. The last mod to change a setting will win.

## Custom SVG Assets

Custom SVG assets can be added through the `SVGAsset` custom type. Any files at these paths will be added to the HBS DataManager and can be referenced through a loadrequest on the datamanager.
  
Simply define the path to your SVGs:
  
```json    
{ "Type": "SVGAsset", "Path": "icons/" },
```

then in your DLL mod read them from the DataManager:

```csharp    
  
# Load the file
DataManager dm = UnityGameInstance.BattleTechGame.DataManager;
LoadRequest loadRequest = dm.CreateLoadRequest();
loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, "icon_foo", null);  
loadRequest.ProcessRequests();  
  
...  

# Read it
SVGAsset icon = DataManager.GetObjectOfType<SVGAsset>("icon_foo", BattleTechResourceType.SVGAsset);

```

## Custom Tags and TagSets

ModTek 0.7.8 and above supports adding and update Tags and TagSets in the MetadataDatabase. HBS BT uses Tags for many different purposes, such as the pilot attribute descriptors, contract validation, and many more. You can add your own custom tags or update existing by adding the `CustomTag` and `CustomTagSet` type to your Manifest element: 
  
```json  
"Manifest": [ 
  	{ "Type": "CustomTag", "Path": "tags/" },
	{ "Type": "CustomTagSet", "Path": "tagSets/" }
],
```

(!) There is currently no way to delete CustomTags or CustomTagSets. These are modified in the ModTek MDDB copy (located in `BATTLETECH\Mods\.modtek\Database\`) and should not alter the base MDDB located in the `BATTLETECH\Data` directory.

Each CustomTag needs to be defined in a .json file with the following structure:  
```json  
{
"Name" : "TAG_NAME_HERE",
"Important" : false,
"PlayerVisible" : true,
"FriendlyName" : "FRIENDLY_NAME",
"Description" : "DESCRIPTION TEXT"  
}
```

You can overwrite HBS defined tags with your own values by using the name tag-name. If multiple mods write to the same tag, the *last* mod to write the tag wins.

Each CustomTagSet needs to be defined in a .json file with the following structure:

```json  
{
	"ID" : "TAGSET_ID",
	"TypeID" : SEE_BELOW,
	"Tags" : [ "TAG_1" , "TAG_2", "TAG_3" ]  
}
```

As with CustomTags, you can update HBS defined TagSets by using the same ID for your  CustomTag. Any existing tags will be removed, and replaced with the tags you defined instead. You **must** include all tags you want in the updated TagSet in your CustomTag definition.

Note that the TypeID is an enumeration type, that unfortunately was never upgraded to a data-driven enum. Thus you are limited to the following enum types, as defined in the Assembly-CSharp.dll. You **must not** use a TypeID that's not defined below, or you will experience erratic behaviors. If the internal logic cannot cast your specified value to the defined enum it will likely fail without printing any error message.


| TagSetType | TypeID |
| -- | -- |
| UNDEFINED | 1 |
| Map | 2 |
| Encounter | 3 |
| Contract | 4 |
| LanceDef | 5 |
| UnitDef | 6 |
| PilotDef | 7 |
| RequirementDefRequirement | 8 |
| RequirementDefExclusion | 9 |
| BiomeRequiredMood | 10 |
| Mood | 11 |
| EventDefRequired | 12 |
| EventDefExcluded | 13 |
| EventDefOptionRequired | 14 |
| EventDefOptionExcluded | 15 |
| EventDefAdded | 16 |
| EventDefRemoved | 17 |
| UnitDef_RequiredToSpawnCompany | 18 |



## Custom Sounds

since 0.7.6.7 ModTek supports loading Wwise sound banks definitions
  example:
```
{
  "name": "Screams",
  "filename": "Screams.bnk",
  "type": "Combat",
  "volumeRTPCIds":[2081458675],
  "volumeShift": 0,
  "events":{
    "scream01":147496415
  }
}
```
  **name** - is unique name of sound bank<br/>
  **filename** - is name of file containing real audio content. Battletech is using Wwise 2016.2<br/>
  **type** - type of sound bank. <br/>
    Available types:<br/>
* **Combat** - banks of this type always loading at combat start and unloading at combat end. Should be used for sounds played on battlefield.<br/>
* **Default** - banks of this type loading at game start. Moistly used for UI <br/>
* **Voice** - banks of this type contains pilot's voices<br/>

**events** - map of events exported in bank. Needed for events can be referenced from code via WwiseManager.PostEvent which takes this name as parameter<br/>
**volumeRTPCIds** - list of RTPC ids controlling loudness of samples. Combat sound banks controlled by effect settings volume slider, Voice by voice<br/>

## Custom Video
  
TODO

## Dynamic Enums
  dynamic enums handled outside manifest array. By DataAddendumEntries
  name - name of type. Supporting types BattleTech.FactionEnumeration, BattleTech.WeaponCategoryEnumeration, BattleTech.AmmoCategoryEnumeration, BattleTech.ContractTypeEnumeration
  path - path to file relative to mod root folder. Examples for content at BattleTech_Data\StreamingAssets\data\enums\ .

  example:
```
  "DataAddendumEntries":[
    {
      "name": "BattleTech.FactionEnumeration",
      "path": "Faction.json"
    },
    {
      "name": "BattleTech.WeaponCategoryEnumeration",
      "path": "WeaponCategory.json"
    }
  ]
```

## Manifest Manipulation

see [Manifest](MANIFEST.md)
