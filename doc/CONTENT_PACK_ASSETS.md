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
