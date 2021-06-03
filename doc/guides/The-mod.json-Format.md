**tl;dr:** *Use this base `mod.json` template to start off with. Every field except `Name` is optional.*

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "Version": "0.1.0",
    "Description": "An awesome mod to do awesome things",
    "Author": "mpstark",
    "Website": "www.github.com/BattletechModders/ModTek",
    "Contact": "fakeemail@fakeemail.com",

    "DLL": "MyAwesomeMod.dll",
    "DLLEntryPoint": "MyAwesomeNamespace.MyCoolClass.MySweetMethod",
    "Settings": {
        "DoCoolStuff": true,
        "BadStuffToAvoid": 1000
    },

    "Manifest": [
        { "Type": "MechDef", "Path": "MyMechDefs" },
        { "Type": "ChassisDef", "Path": "MyChassisDefs/chassisdef_specific_chassis.json" }
    ]
}
```

# The ModTek `mod.json` format

Every ModTek mod needs a `mod.json` folder in the root of its directory i.e. `\BATTLETECH\Mods\MyAwesomeMod\mod.json`. This file contains information about your mod, serves as a table of contents, and tells ModTek how your mod should be loaded.

The only field in this file that is *explicitly* required is the `Name`, which serves as a ***unique identifier*** for your mod for each given user's install. If two mods have the same name and the games tries to launch, the first mod that loads wins and the second mod simply isn't loaded.

The simplest possible `mod.json`:

```json
{
    "Name": "MyAwesomeMod"
}
```

## Optional Fields

### Enabled

Next, is the `Enabled` field, which will tell ModTek to either load your mod or to ignore it. If the `Enabled` field is missing, the mod is loaded, but you should always include this if you plan on giving this mod out so that, in the event that they'd like to toggle your mod off for a bit, the field is already there.

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true
}
```

### Information

These are optional informational fields that ModTek doesn't do anything with. In the future, they might be displayed in a UI somewhere and they are useful to users for informations sake.

|Field|Type|Description|
|:---|:---:|---|
|Version|string|Version of this mod|
|Description|string|A short description of this mod|
|Author|string|Author of this mod|
|Website|string|The website that users should expect to find downloads for this mod and support|
|Contact|string|Contact of the mod's author|
|PackagedOn|DateTime|The date/time that this mod was packed on|

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "Version": "0.1.0",
    "Description": "An awesome mod to do awesome things",
    "Author": "mpstark",
    "Website": "www.github.com/BattletechModders/ModTek",
    "Contact": "fakeemail@fakeemail.com",
    "PackagedOn": "2018-06-15T00:00:00Z"
}
```

### BattleTech Version

These fields determine if your mod will be loaded based on the BattleTech version. These were introduced in 0.5.1.

|Field|Type|Description|
|:---|:---:|---|
|BattleTechVersion|string|The game version that this mod supports -- if this field is present, min and max will be ignored|
|BattleTechVersionMin|string|The minimum version that this mod supports|
|BattleTechVersionMax|string|The maximum version that this mod supports|

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,
    "BattleTechVersion": "1.4",
}
```

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,
    "BattleTechVersionMin": "1.2",
    "BattleTechVersionMax": "1.3.2",
}
```

### Dependancies

These fields determine if ModTek should load your mod based on the other installed mods, and what order they should be loaded in.

|Field|Type|Description|
|:---|:---:|---|
|DependsOn|array\<string\>|Don't load this mod if these mods are not loaded and load my mod after these mods|
|OptionallyDependsOn|array\<string\>|Load this mod *after* these mods if they're in the load order|
|ConflictsWith|array\<string\>|Don't load this mod if these mods are in the load order|

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "DependsOn": [ "CoolDep", "CoolDep2" ],
    "ConflictsWith": [ "EvilMod" ],
    "OptionallyDependsOn": [ "MaybeDep", "MaybeDep2" ]
}
```

### Content

|Field|Type|Description|
|:---|:---:|---|
|Manifest|array\<object\>|Entries that will be added to the game|
|RemoveManifestEntries|array\<string\>|Entries that will be removed from the game *(added in 0.7.0/removed in v2.0)*|
|CustomResourceTypes|array\<string\>|Custom resource types that this mod adds or consumes *(added in 0.7.0)*|
|LoadImplicitManifest|bool|If the manifest should include the implicit StreamingAssets and ContentPackMerges directory entries, *default true*|

Each entry is a JObject with the following fields:

|Field|Type|Description|
|:---|:---:|---|
|Path|string|*required* Relative path to the entry in the mod folder, if this is a directory, all files inside of that file|
|Type|string|*required* Type of the entry, must be a BattleTechResourceType or a CustomResourceType|
|Id|string|ID of this entry, if not provided, this is assumed based on JSON parsing or file name|
|AssetBundleName|string|The asset bundle that this entry is in (generally used for models)|
|AssetBundlePersistent|bool||
|AddToAddendum|string|The VersionManifest addundum that this entry should be added to, if not provided, not added to any addendum|
|AddToDB|bool|If this entry should be added to the MetadataDatabase (MDD/MDDB), only for particular types|
|RequiredContentPacks|array\<string\>|A list of content packs the entry requires, valid values are: shadowhawkdlc, flashpoint, urbanwarfare and heavymetal|
|ShouldMergeJSON|bool|If this .json file should be merged to an existing entry|
|ShouldAppendText|bool|If this .csv/.txt file should be appended to an existing entry *(added in 0.7.0)*|

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "Manifest": [
        { "Type": "MechDef", "Path": "MyMechDefs" },
        { "Type": "MechDef", "Path": "MyMechDefMerges", "ShouldMergeJSON": true }
        { "Type": "ChassisDef", "Path": "MyChassisDefs/chassisdef_specific_chassis.json" }
    ]
}
```

#### ModTek's Built-in Custom Resource Types

Custom resource types were added in 0.7.0.

|Type|Description|
|:---|---|
|Video|Video to be played by the game, generally by milestone or event *(replace only)*|
|GameTip|Tips that play in the loading screens *(append/replace)*|
|SoundBank|Sounds for the game *(replace only)*|
|DebugSettings|Debug settings for the game *(merge JSON/replace)*|
|AdvancedJSONMerge|Finer control of json merges *(add only)*|

### DLL

|Field|Type|Description|
|:---|:---:|---|
|DLL|string|Path to .dll file that should be loaded relative to the mod's directory|
|DLLEntryPoint|string|Optional, the method that you want ModTek to call when your DLL is loaded|
|EnableAssemblyVersionCheck|bool|Optional, if this assembly should be resolved no matter what version that other .`dll`s were linked against *(default true)* *(added in 0.6.0)*|

If `DLL` is provided and `DLLEntryPoint` is omitted, all public, static methods named `Init` will be called.

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "DLL": "MyAwesomeMod.dll",
    "DLLEntryPoint": "MyAwesomeNamespace.MyCoolClass.MySweetMethod"
}
```

### Settings

If your mod includes a DLL, then it can be passed a string containing settings from the `mod.json` file. 

```json
{
    "Name": "MyAwesomeMod",
    "Enabled": true,

    "Description": "An awesome mod to do awesome things",
    "Author": "mpstark",
    "Website": "www.github.com/BattletechModders/ModTek",
    "Contact": "fakeemail@fakeemail.com",

    "DLL": "MyAwesomeMod.dll",
    "DLLEntryPoint": "MyAwesomeNamespace.MyCoolClass.MySweetMethod",

    "Settings": {
        "DoCoolStuff": true,
        "BadStuffToAvoid": 1000
    }
}
```

You should generally deserialize this using `Newtonsoft.Json.JsonConvert.DeserializeObject`. This library is provided by the game at version 10, here's a simple version of doing so.

```csharp
using Newtonsoft.Json;

class Settings
{
    public bool DoCoolStuff = false;
    public int BadStuffToAvoid = 500;
}

public static class MyAwesomeMod
{
    private Settings _settings;
    public static void Init(string modDirectory, string settingsJson)
    {
        try
        {
            // deserialize settings json onto our settings object
            _settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
        }
        catch (Exception)
        {
            // use default settings
            _settings = new Settings();
        }

        //...
    }
}
```

### Misc.

|Field|Type|Description|
|:---|:---:|---|
|IgnoreLoadFailure|bool|Should ModTek report when this mod doesn't load?|
