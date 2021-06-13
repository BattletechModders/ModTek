# Custom Debug Settings

HBS defined various configuration options in the `BATTLETECH\BattleTech_Data\StreamingAssets\data\debug\settings.json` file. Internal logger levels are defined here, as are some configuration settings only intended to be used by developers. Mods can modify these values using the `DebugSettings` custom type. The contents of any such files will be merged into the HBS provided settings.json. 

This custom type is last-in, last-out. The last mod to change a setting will win.
