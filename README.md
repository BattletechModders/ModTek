# ModTek

ModTek is a mod-loader for [HBS's BattleTech PC game](https://harebrained-schemes.com/battletech/). It allows modders to create self-contained mods that do not over-write game files. ModTek is run at game startup and dynamically loads mods that conform to the [mod.json format](https://github.com/BattletechModders/ModTek/wiki/The-mod.json-format). Mod dependencies are resolved and load order enforced without needing to edit the `VersionManifest.csv`.

Since BattleTech 1.7, HBS introduced their own mod-loader based on an older ModTek version. It is missing many features of newer ModTek versions, including DLC support.

# Installing ModTek 3.0.0 or later

Installation of ModTek is straightforward for Windows. You download the `ModTek.zip` file and extract it to the directory of the game.

1. Download the [latest stable release from github](https://github.com/BattletechModders/ModTek/releases).
1. Extract the contents of the zip to `BATTLETECH/` so that the `Mods/` folder in the zip appears as `BATTLETECH/Mods/` and the [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) files (winhttp.dll etc..) appear directly under `BATTLETECH/`.

> **Note**
> `BATTLETECH/` refers to the installation folder where `BattleTech.exe` can be found.

On game startup, ModTek decorates the version number found in the bottom left corner of the main menu with `/W MODTEK`. If you don't see this something has gone wrong.

## Linux

The zip contains the UnityDoorstop script `run.sh` and libraries to run the game with.

### Steam on Linux

> **Note**
> These instructions are based on the [Steam Guide for BepInEx](https://docs.bepinex.dev/master/articles/advanced/steam_interop.html).

Instead of running the `run.sh` script directly, you need to ask Steam to run it for you.

Right mouse click on the game in the Steam library -> `Properties...` -> `SET LAUNCH OPTIONS`.

Launch options for Linux:
> `./run.sh %command%`

### GOG on Linux

Modify the `start.sh` script from GOG to execute the `run.sh` script from ModTek/UnityDoorstop instead of `BattleTech`.

> ```
> #chmod +x "BattleTech"
> #./"BattleTech"
> ./run.sh
> ```

### Proton/Wine on Linux

Using Proton or Wine is also supported, make sure the `winhttp.dll` from UnityDoorstop is loaded by setting the override to `native, builtin`.

## macOS

> **Note**
> The installation instructions for macOS are similar to Linux, only differences are listed here.

The base installation folder is the `Contents/Resources` directory within the .app Application packages.
For a standard Steam installation that means the following path:
> `~/"Library/Application Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/"`

### Steam on macOS

Launch options for macOS need to contain the absolute path to the run script.
In a terminal, run this from the same location where the run script is:
> `echo "\"$(pwd)/run.sh\" %command%"`

The launch options should then look something like this:
> `"/Users/ReplaceThisByYourUsername/Library/Application Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/run.sh" %command%`

# Further Documentation

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
- [Preloader and Injectors](doc/PRELOADER.md)

## Developing ModTek

Information on how to build and release ModTek is documented in [DEVELOPER.md](DEVELOPER.md).

## Enabling or Disabling

ModTek 0.7.6 or higher can be enabled or disabled from within the in-game mods menu. If ModTek is enabled, the  "MODS ENABLED" check box will always be set to enabled. To disable ModTek look through the mod list until you find 'ModTek', and disable that 'mod'. Restart the game, and only the in-game mod-loader will be used. Repeat the process but enable the 'ModTek' mod to re-enable an external ModTek install. 

> **Warning**
> You must restart the game to enable or disable an external ModTek!

## What is UnityDoorstop and what those files like winhttp.dll

[Unity DoorStop](https://github.com/NeighTools/UnityDoorstop) provides a set of files that trick Unity into loading ModTek, but without modifying the game files.
The old way was asking the user to run a `ModTekInjector.exe` manually, which modified the game so it loaded ModTek.
In cases of updates, the old way required the user to "verify" the game files and re-install mods, but using UnityDoorstop this is not necessary anymore.

## License

ModTek is provided under the [Unlicense](UNLICENSE), which releases the work into the public domain.
