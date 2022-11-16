# ModTek

ModTek is a mod-loader for [HBS's BattleTech PC game](https://harebrained-schemes.com/battletech/). It allows modders to create self-contained mods that do not over-write game files. ModTek is run at game startup and dynamically loads mods that conform to the [mod.json format](https://github.com/BattletechModders/ModTek/wiki/The-mod.json-format). Mod dependencies are resolved and load order enforced without needing to edit the `VersionManifest.csv`.

Since BattleTech 1.7, HBS introduced their own mod-loader, which is based on an older ModTek version. It is missing many features of newer ModTek versions, including DLC support.

# Topics

- [Installation Instructions](INSTALL.md)
- [Changelog](doc/CHANGES.md)
- [How to contribute to ModTek](doc/CONTRIBUTE.md).

## Modding

- [A Brief Primer on Developing ModTek Mods](doc/PRIMER.md)
- [The mod.json Format](doc/MOD_JSON_FORMAT.md)

### JSON Modding

- [Writing ModTek JSON mods](doc/MOD_JSON.md)
- [Advanced JSON Merging](doc/ADVANCED_JSON_MERGING.md)
- [Manifest Manipulation](doc/MANIFEST.md)
- [Dynamic Enums / DataAddendumEntries](doc/DATA_ADDENDUM_ENTRIES.md)
- [Content Pack Assets](doc/CONTENT_PACK_ASSETS.md)

### DLL Modding

- [Writing ModTek DLL mods](doc/MOD_DLL.md)
- [Development Guide](doc/DEVELOPMENT_GUIDE.md)
- [Preloader and Injectors](doc/PRELOADER.md)
- [Logging with ModTek](doc/LOGGING.md)

### Custom Types

- [DebugSettings](doc/CUSTOM_TYPE_DEBUGSETTINGS.md)
- [SVGAssets](doc/CUSTOM_TYPE_SVGASSET.md)
- [Custom Tags and Tagsets](doc/CUSTOM_TYPE_CUSTOMTAGS.md)
- [SoundBanks](doc/CUSTOM_TYPE_SOUNDBANKS.md)
- Custom Video - TBD
- Assembly - TBD
