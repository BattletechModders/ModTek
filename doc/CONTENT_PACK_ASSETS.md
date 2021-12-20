# ContentPackAssets

a.k.a. DLC content support

If you want to merge changes to base game content, you can use `StreamingAssets`:
> mods/{MOD}/StreamingAssets/{ID}.{FILE_EXTENSION}

Example:
> mods/YourMod/StreamingAssets/chassisdef_atlas_AS7-D-HT.json

DLC content works the same way, just add the file to the StreamingAssets folder.

Example:
> mods/YourMod/StreamingAssets/chassisdef_annihilator_ANH-JH.json

Variables:
- {MOD}: The name of your mod.
- {ID}: The identifier of the resource, is the same as the filename without the file extension.
- {FILE_EXTENSION}: .json .csv and .txt are valid values.

Visit [design-data](https://github.com/caardappel-hbs/bt-dlc-designdata) to find extracted DLC jsons.

## Additional content

To add new resources to the game if a DLC loaded, one can use the `RequiredContentPacks` field in a manifest entry.

Example:
```json
{
    "Name": "MyModForHeavyMetal",
    "Enabled": true,
    "Manifest": [
        { "Type": "MechDef", "Path": "MyMechDefs", "RequiredContentPacks": ["heavymetal"] }
    ]
}
```

In the example, the MechDefs found at the path are only available in the game when the DLC `heavymetal` is available.

Possible content pack names:
- flashpoint
- shadowhawkdlc
- urbanwarfare
- heavymetal

Note that `RequiredContentPacks` is only supported for newly added resources.
Merging, appending or replacing resources won't allow to change `RequiredContentPacks` of existing resources.
Custom resources do not support `RequiredContentPacks`.

## Implementional details for DLL modders

As with original DLC content, ModTek adds additional DLC dependent resources to the BattleTechResourceLocator and indexes them into the MDDB.

When querying the `BattleTechResourceLocator`, make sure to supply the `filterByOwnership` flag with a value of `true`.
Otherwise use the method `IsResourceOwned` from `ContentPackIndex` to check if a resource id is owned and should be processed.

Note that ownership of DLC content can change during the game, e.g. if someone logs in or out of a Paradox account from the main menu.
