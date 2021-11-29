# ModTek

ModTek is a mod-loader for [HBS's BattleTech PC game](https://harebrained-schemes.com/battletech/). It allows modders to create self-contained mods that do not over-write game files. ModTek is run at game startup and dynamically loads mods that conform to the [mod.json format](https://github.com/BattletechModders/ModTek/wiki/The-mod.json-format). Mod dependencies are resolved and load order enforced without needing to edit the dreaded `VersionManifest.csv`. It also provides for incremental patching of stock game files that are easy to remove, version, and persist through patches.

In version 1.7 HBS introduced an internal mod-loader. The in-game mod-loader shares many similarities to ModTek, but has fewer features and less robust handling. We strongly recommend using a stand-alone copy of ModTek instead of the in-game mod-loader. You'll have a better experience and be more in-line with current community best practices.

# Installing

Installation of ModTek is straightforward. You download a .zip file, extract it and run an injection utility.

1. Download a [release from here](https://github.com/BattletechModders/ModTek/releases).
1. If the `BATTLETECH\Mods\` directory doesn't exist, create it.
1. Move the entire ModTek folder from the release download into the `BATTLETECH\Mods\` folder
1. You should now have a `BATTLETECH\Mods\ModTek\` folder
1. Run the injector `ModTekInjector.exe` in `BATTLETECH\Mods\ModTek\`. This code to `BATTLETECH\BattleTech_Data\Managed\Assembly-CSharp.dll` which will launch ModTek at startup.

:no_entry: DO NOT move anything from the `BATTLETECH\Mods\ModTek\` folder, it is self-contained.

:warning: `BATTLETECH\Mods\` is in game installation folder NOT in `Documents\My Games`

On game startup, ModTek decorates the version number found in the bottom left corner of the main menu with "/W MODTEK". If you don't see this something has gone wrong.

# Further Documentation

- [The Drop-Dead-Simple-Guide to Installing BTML & ModTek & ModTek mods](doc/QUICKSTART.md)
- [A Brief Primer on Developing ModTek Mods](doc/PRIMER.md)
- [The mod.json Format](doc/MOD_JSON_FORMAT.md)
- [Writing ModTek JSON mods](doc/MOD_JSON.md)
- [Writing ModTek DLL mods](doc/MOD_DLL.md)
- [Advanced JSON Merging](doc/ADVANCED_JSON_MERGING.md)
- [DebugSettings](doc/CUSTOM_TYPE_DEBUGSETTINGS.md)
- [SVGAssets](doc/CUSTOM_TYPE_SVGASSET.md)
- [Custom Tags and Tagsets](doc/CUSTOM_TYPE_CUSTOMTAGS.md)
- [SoundBanks](doc/CUSTOM_TYPE_SOUNDBANKS.md)
- Custom Video - TBD
- [Dynamic Enums / DataAddendumEntries](doc/DATA_ADDENDUM_ENTRIES.md)
- [Manifest Manipulation](doc/MANIFEST.md)
- [Content Pack Assets](doc/CONTENT_PACK_ASSETS.md)

## Developing ModTek

Information on how to build and release ModTek is documented in [DEVELOPER.md](DEVELOPER.md).

## Enabling or Disabling

ModTek 0.7.6 or higher can be enabled or disabled from within the in-game mods menu. If ModTek is enabled, the  "MODS ENABLED" check box will always be set to enabled. To disable ModTek look through the mod list until you find 'ModTek', and disable that 'mod'. Restart the game, and only the in-game mod-loader will be used. Repeat the process but enable the 'ModTek' mod to re-enable an external ModTek install. 

:warning: You must restart the game to enable or disable an external ModTek!

## What is the Injector

Some people worry about running `ModTekInjector.exe` as it's an unknown, unsigned executable file. This file is a small program that injects ModTek code into HBS code before the main game-loop gets started. This allows ModTek to load mods that modify before Unity takes control of everything. It's similar in concept to [BepInEx](https://github.com/BepInEx/BepInEx.UnityInjectorLoader), [Unity DoorStop](https://github.com/NeighTools/UnityDoorstop), and related programs that enable modding before the main Unity thread begins.

## Handling Game Updates

Generally, updates can be dealt with by re-running `ModTekInjector.exe` -- though sometimes ModTek will have to if certain game API's change. If the game has changed in a way that breaks an individual mod, that mod will need to be updated, but generally, this will only happen in large updates.


## License

ModTek is provided under the [Unlicense](UNLICENSE), which releases the work into the public domain.
