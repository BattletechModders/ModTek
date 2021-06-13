# ContentPackAssets

a.k.a. DLC content support

If you want to merge changes to base game content, you can use `StreamingAssets`:
> mods/{MOD}/StreamingAssets/{ID}.{FILE_EXTENSION}

Example:
> mods/YourMod/StreamingAssets/chassisdef_atlas_AS7-D-HT.json

For merging DLC content, you can now use this directory structure:
> mods/{MOD}/ContentPackAssets/{CONTENT_PACK_ID}/{RESOURCE_TYPE}/{ID}.{FILE_TYPE}

Example:
> mods/YourMod/ContentPackAssets/heavymetal/ChassisDef/chassisdef_annihilator_ANH-JH.json

Variables:
- {MOD}: The name of your mod.
- {ID}: The identifier of the resource, is the same as the filename without the file extension.
- {FILE_EXTENSION}: .json .csv and .txt are valid values.
- {CONTENT_PACK_ID}: Any of shadowhawkdlc, flashpoint, urbanwarfare and heavymetal.
- {RESOURCE_TYPE}: See the generated Manifest.csv file under Mods/.modtek for types and ids. To see the original content, visit [design-data](https://github.com/caardappel-hbs/bt-dlc-designdata) .

You can also define additional content in this folder, if a user does not own the relevant content pack, that additional content is not loaded.
Basically just fills out RequiredContentPacks as seen in [Mod JSON Format](MOD_JSON_FORMAT.md) for you.
