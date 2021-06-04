First, download the [latest release here](https://github.com/BattletechModders/ModTek/releases).

# ModTek 0.6.0 and after

BTML has been integrated with ModTek in the 0.6.0 release.

* If the `BATTLETECH\Mods\` directory doesn't exist, create it.
* Move the entire ModTek folder from the release download into the `BATTLETECH\Mods\` folder
* You should now have a `BATTLETECH\Mods\ModTek\` folder
* Run the injector program (`ModTekInjector.exe`) in that folder
* DO NOT move anything from the `BATTLETECH\Mods\ModTek\` folder, it is self-contained.

## For macOS

1. Use the following directory instead of the BATTLETECH directory: ~/Library/Application\ Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/
2. If the Mods directory doesn't exist, create it here: ~/Library/Application\ Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/Mods/
3. Move the entire ModTek folder from the release download into the /Mods/ folder created above.
4. You should now have a ~/Library/Application\ Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/Mods/ModTek/ folder
5. Run the injector program (ModTekInjector.exe) in that folder. To do this:
	a. Open a Terminal window.
	b. At the command line, type "cd ~/Library/Application\ Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/Mods/ModTek" and press Return.
	c. At the command line, type "mono ModTekInjector.exe" then press Return to run the injector.
6. DO NOT move anything from the /Mods/ModTek/ folder, it is self-contained.

# ModTek 0.5.1 and before

# Installing BattleTech Mod Loader

BTML releases are hosted on GitHub [here](https://github.com/BattletechModders/BattleTechModLoader/releases). Make sure that you are downloading the actual release (will be something like "BTML-v0.3.0.zip") instead of the source. Check the zip file, it should contain an `.exe` and a couple `.dll`s.

Find where BattleTech has been installed to. You're looking for a folder called `BATTLETECH`. Default paths on windows are:

* Steam: `steamapps\common\BATTLETECH\`
* GOG Galaxy: `GOG Galaxy\Games\BATTLETECH\`

On MacOS:
* Steam: `~/Library/Application\ Support/Steam/steamapps/common/BATTLETECH/BattleTech.app`
* GOG Galaxy: \<help wanted\>

In this directory, there will be a `Managed` directory: `BATTLETECH\BattleTech_Data\Managed\` (`BattleTech.app/Contents/Resources/Data/Managed/` on MacOs). It will have lots of `.dll` files in it, including the one that we'll be injecting into, `Assembly-CSharp.dll`.

Drag all of the files from the downloaded BTML zip into this folder. There should be `0Harmony.dll`, `BattleTechModLoader.dll` and `BattleTechModLoaderInjector.exe`. They are all necessary.  Run `BattleTechModLoaderInjector.exe` after it has been moved into the `Managed` directory. It'll pop open a console window and inject into `Assembly-CSharp.dll` after backing it up. 

## MacOS, Linux, and Command Line Users
If you are comfortable in a command line interface (or on *nix), you can run `BattleTechModLoaderInjector.exe` with the `/help` parameter to see all your options. Additionally, you will see an option to run the injector executable and give it a manged folder parameter, so you can run the injector outside the managed folder. This is necessary on *nix unless you want to move DLLs around in the `Managed` folder. Additionally, on MacOS and Linux you will run the injector via the `mono` command: `mono BattleTechModLoaderInjector.exe`.

## Post-Injection

BTML is now installed. Whatever you do, do not ever put `BattleTechModLoader.dll` or `0Harmony.dll` into the BattleTech `\Mods` folder. This will create an infinite loop of BTML loading itself -- it's not pretty.

The `Assembly-CSharp.dll` file may be a little smaller after injecting. That's normal.  

## Uninstalling

You can delete the injected file and rename the `.orig` file back, or run `BattleTechModLoaderInjector.exe /restore` from a command prompt with `Win+R` and type `cmd <enter>` (or via Terminal.app` on MacOS).  Deleting the `.orig` file will invalidate the `/restore` feature.

# ModTek

Download `ModTek-vX.X.X.zip` (the Xs will be a specific version number) from the GitHub Releases page [here](https://github.com/BattletechModders/ModTek/releases). Current releases should contain the `ModTek.dll` assembly and a unity asset file called `modtekassetbundle`.

Navigate back to `BATTLETECH\`. If there isn't a `BATTLETECH\Mods\` folder, create it (this folder will be `BattleTech.app/Contents/Resources/Mods` on MacOS). Move `ModTek.dll` and `modtekassetbundle` into the `Mods` folder.

ModTek is now installed. If BTML is also correctly installed and injected, ModTek will add a "/W MODTEK" with the ModTek version number to the game's main menu version number in the bottom left.  If you don't see ModTek list in the bottom right corner, BTML is likely not injected or you put ModTek.dll in the wrong place.  Double-check the previous instructions.

# Installing a ModTek Mod

Download the ModTek mod that you want to install. All ModTek mods exist in their own subfolders inside `BATTLETECH\Mods`.  If you unzip the mod, ensure that it goes into its own folder.

The resulting mod-folder must have a `mod.json` file in the first level -- something like this: `BATTLETECH\Mods\ModThatYouJustDownloaded\mod.json`. If it does not, then something is wrong, maybe your mod-folder contains the actual mod folder. If this is the case, just move the contents down a level.

Your ModTek mod is now installed.


# Compatibility Notes

## BTML
All [mods included in RogueTech](https://www.nexusmods.com/battletech/articles/27) are compatible with it, but other mods may be not.
For example they may crash if they reference game constants during mod initialisation.

New and old BTMLs cannot be used together.
This new BTML will detect whether old BTML is already in use, but the old one will not.
If you want to downgrade after trying the new version, please revert the original dll first.

## ModTek

One of the features enabled by the earlier load is a loading screen that shows the progress of mod load.
It is important when the player have multiple big mods installed, such as [RogueTech](https://www.nexusmods.com/battletech/mods/79), which combines hundreds of new components with thousands of new star systems.