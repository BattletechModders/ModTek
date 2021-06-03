*Before you read any of this, you should know that if HBS' base JSON is invalid (excepting for their comments that they pull out during runtime, and trailing commas, which are ignored), ModTek cannot patch it. ModTek tries to automatically fix missing commas.*

ModTek makes mods that change JSON values or add new JSON files far, far better for the end-user. Instead of manually editing existing game files or replacing them with the ones that you provide, they can simply drag and drop your mod folder into the `/BATTLETECH/Mods/` directory and ModTek will take care of the rest, both by adding new JSON files into the manifest and by merging JSON that matches existing files onto the game JSON. Uninstalling your mod or updating it is as simple as deleting the existing folder and/or replacing it.

Developing a JSON mod for ModTek is fairly straightforward and in this article/guide, we'll build three different mods. First, we'll make a simple mod that changes the `StreamingAssets/data/constants/AudioConstants.json` file as suggested by [Alec Meer at RockPaperShotgun](https://www.rockpapershotgun.com/2018/05/02/battletech-speed-fix/) based on community findings. After that, we'll build a mod that changes a hardpoint on a mech chassis. Finally we'll add new variants of an existing chassis.

For all of the examples, we will first need to write a `mod.json` file for ModTek to read. Much more in-depth documentation for that [is here](The-mod.json-Format.md).

*Note: The log file at the path `BATTLETECH/Mods/.modtek/ModTek.log` is very helpful for figuring out if something has worked or not. There is also the game log located at `BATTLETECH/BattleTech_Data/output_log.txt` that can tell you if something broke in the game.*

## EliminateAudioDelays, making simple changes to an existing JSON file

As described in the `mod.json` [article](The-mod.json-Format.md), we'll setup ours to look like this:

```json
{
    "Name": "EliminateAudioDelays",
    "Enabled": true,
    "Version": "0.1.0",
    "Description": "Get rid of those delays!",
    "Author": "mpstark",
    "Website": "www.github.com/BattletechModders/ModTek",
    "Contact": "fakeemail@fakeemail.com",
}
```

Since the file that we want to modify already exists in the game at the path `BattleTech_Data/StreamingAssets/data/constants/AudioConstants.json`, we'll simply use ModTek's implicit StreamingAssets mirror folder. Everything in this mirror of the existing files will be loaded *without specifying it in the `mod.json` manifest*, if it's in the same place with the same name. Additionally, you should not use the `StreamingAssets` folder for new files, just ones like `AudioConstants.json` that already exist.

So we'll just copy the existing file over to our mod folder in the path `BATTLETECH/Mods/EliminateAudioDelays/StreamingAssets/data/constants/AudioConstants.json`. Then we'll open that file with our favorite text editor (I love Visual Studio Code with the JSON linter!).

The `AudioConstants.json` file is pretty big and contains a lot of stuff -- I find that the collapsing blocks in my editor help close out things from the file that I don't care about. We're looking for five values, `AttackPreFireDuration`, `AttackAfterFireDelay`, `AttackAfterFireDuration`, `AttackAfterCompletionDuration` and `audioFadeDuration` which control the things that we want to change. Since we only want to change these five things and they're all in the first level (i.e. they're not nested in anything), we can simply delete everything except for those entries and edit them to have our values. Our resulting "partial" JSON file looks something like this:

```json
{
   "AttackPreFireDuration" : 0,
   "AttackAfterFireDelay" : 0,
   "AttackAfterFireDuration" : 0,
   "AttackAfterCompletionDuration" : 0,
   "audioFadeDuration" : 0
}
```

Save the file and your mod is now complete. What happens when BattleTech loads the JSON is that ModTek intercepts it and "merges" your values onto what was loaded in mod load order. So the game initially sees the original file, then it'll merge the first loaded mod that affects that file, then the second, then the third, etc. As you can probably intuit, the *last* mod that changes a value is the value that is chosen, because it wrote over any other mod's value. There are some details about this process for JArrays (the ones that use the `[]` braces and don't have keys) that we'll talk about in the next example.

Also, before you distribute anything, you should probably check it over. VSCode has a linter extension for JSON, but if you have a different editor, you should give it a quick once-over in a linter like [this free online one](https://jsonlint.com/). A linter will try to point out things that are wrong, like you missed a comma somewhere or have a missing brace.

## Changing how many 'Mech pieces it takes to salvage

If we want to change something that is in a second level, we have to include the parent object as well. In `/simGameConstants/SimGameConstants.json`, there is a single value, "DefaultMechPartMax", that changes how many pieces of salvage that it takes to combine and fix up a 'Mech, however, this is part of a larger object, called "Story". If we simply wanted to change this value and nothing else, our partial JSON would look like this:

```json
{
    "Story" : {
        "DefaultMechPartMax" : 5
    }
}
```

Note that even though we specified the "Story" object, we didn't have to include all of the values of it.

## Modding the AS7-D head hardpoint, or, why HBS should have made `Locations` a JObject

Based on the previous examples, you would expect everything to follow the same basic pattern, remove everything that you're not changing, and change the things that you are. And for anything that is a JObject, i.e. the things surrounded by `{}` and that have `"Keys"` on the left, a colon `:` in the middle, and `"Values"` on the right.

Unfortunately, with JArrays, which are surrounded by `[]` braces, things are a little bit more complicated then they could have been. Since they don't have keys, they can't be indexed well, and since their order is arbitrary and could change between game version, they can't be relied upon to be in the same spots. With some simple arrays, you can make assumptions on how a JSON merge should go, but because of the relatively complex nature of BattleTech's JSON, ***any change that you make to an array, replaces that array with yours, including things you didn't edit.***

Our goal is to change the AS7-D's head to have a hardpoint for support called "anti-personal" in the files. In order to do this though, we have to specify the entire locations array, which means that only one mod can change these each arrays at a time.

Here's our modded chassis file for the AS-7D which we would put into our mod folder at `MyModFolder/StreamingAssets/data/chassis/chassisdef_atlas_AS7-D.json`. You'll still obviously need to provide a `mod.json` file as well.

```json
{
    "Locations": [
        {
            "Location": "Head",
            "Hardpoints": [
                {
                    "WeaponMount": "AntiPersonnel",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 1,
            "MaxArmor": 45,
            "MaxRearArmor": -1,
            "InternalStructure": 16
        },
        {
            "Location": "LeftArm",
            "Hardpoints": [
                {
                    "WeaponMount": "Energy",
                    "Omni": false
                },
                {
                    "WeaponMount": "AntiPersonnel",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 8,
            "MaxArmor": 170,
            "MaxRearArmor": -1,
            "InternalStructure": 85
        },
        {
            "Location": "LeftTorso",
            "Hardpoints": [
                {
                    "WeaponMount": "Missile",
                    "Omni": false
                },
                {
                    "WeaponMount": "Missile",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 10,
            "MaxArmor": 210,
            "MaxRearArmor": 105,
            "InternalStructure": 105
        },
        {
            "Location": "CenterTorso",
            "Hardpoints": [
                {
                    "WeaponMount": "Energy",
                    "Omni": false
                },
                {
                    "WeaponMount": "Energy",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 4,
            "MaxArmor": 320,
            "MaxRearArmor": 160,
            "InternalStructure": 160
        },
        {
            "Location": "RightTorso",
            "Hardpoints": [
                {
                    "WeaponMount": "Ballistic",
                    "Omni": false
                },
                {
                    "WeaponMount": "Ballistic",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 10,
            "MaxArmor": 210,
            "MaxRearArmor": 105,
            "InternalStructure": 105
        },
        {
            "Location": "RightArm",
            "Hardpoints": [
                {
                    "WeaponMount": "Energy",
                    "Omni": false
                },
                {
                    "WeaponMount": "AntiPersonnel",
                    "Omni": false
                }
            ],
            "Tonnage": 0,
            "InventorySlots": 8,
            "MaxArmor": 170,
            "MaxRearArmor": -1,
            "InternalStructure": 85
        },
        {
            "Location": "LeftLeg",
            "Hardpoints": [],
            "Tonnage": 0,
            "InventorySlots": 4,
            "MaxArmor": 210,
            "MaxRearArmor": -1,
            "InternalStructure": 105
        },
        {
            "Location": "RightLeg",
            "Hardpoints": [],
            "Tonnage": 0,
            "InventorySlots": 4,
            "MaxArmor": 210,
            "MaxRearArmor": -1,
            "InternalStructure": 105
        }
    ]
}
```

Even though we didn't change anything other than add that single hardpoint to the head, we still had to restate the entire array. Which kind of sucks.

## Adding new 'Mech loadouts

As always, each individual mod that you ship out as a mod folder needs a `mod.json` file. In the previous cases, we didn't need to specify a manifest because we were changing existing game files. If we want to add new files, then we need to add a manifest to the `mod.json` file.

Say that we wanted to add some 'Mech loadouts for existing chassis. After writing the Mechdef files, with standard BattleTech modding, you'd have to add each to the `VersionManifest.csv` or add an addendum. You can think of the `mod.json` manifest as exactly the same thing, except you don't have to use `.csv`, you don't have to fake a time and date, you don't have to count commas, and most importantly ***you can specify entire folders for a single type***.

*Note: The `StreamingAssets` folder should not contain new entries, just ones from the base game that you want to modify, even if you point at them in the manifest.*

```json
{
    "Name": "TheGreatestMech",
    "Enabled": true,
    "Manifest": [
        { "Type": "MechDef", "Path": "MyMechs" }
    ]
}
```

This will now add all of the `.json` files in the `MyModFolder/MyMechs` to the `VersionManifest` at runtime automatically. Pretty sweet huh?

### A corollary to this

Using the same mechanism, you can add to a specific manifest addendum or create a manifest addendum by using the `AddToAddendum` field in each manifest entry. This is particularly useful for adding emblems to the game, as "deim0s" from the BattleTech Reddit's Discord channel discovered. Since emblems need to be added twice here, once as a type "Sprite" and once as a type "Texture2D".


```json
{
    "Name": "MyEmblems",
    "Enabled": true,
    "Manifest": [
        { "Type": "Texture2D",  "Path": "emblems", "AddToAddendum": "PlayerEmblems" },
        { "Type": "Sprite",     "Path": "emblems", "AddToAddendum": "PlayerEmblems" }
    ]
}
```