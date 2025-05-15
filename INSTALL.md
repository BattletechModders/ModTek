
# Installing ModTek v3

> **Note**
> ModTek v3 or later does **not** have a `ModTekInjector.exe` anymore, and instead it uses 
> [UnityDoorstop](https://github.com/BattletechModders/UnityDoorstop), which is based on hooking via libraries.
> OS platforms (Windows, Linux, Mac) and game distribution platforms (Steam, GOG) are supported.

> **Warning**
> For ModTek to work properly, BATTLETECH should be installed outside of the program files folder structure,
> as that is protected by [UAC](https://en.wikipedia.org/wiki/User_Account_Control) and therefore interferes with ModTek.

Installation of ModTek is straightforward for Windows. You download the `ModTek.zip` file and extract it to the directory of the game.

1. Download the [latest stable release from github](https://github.com/BattletechModders/ModTek/releases).
1. Extract the contents of the zip as-is to `BATTLETECH/` so that the `Mods/` folder in the zip appears as `BATTLETECH/Mods/` and the [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) files (winhttp.dll etc..) appear directly under `BATTLETECH/`.

> **Note**
> `BATTLETECH/` refers to the installation folder where `BattleTech.exe` can be found.

On game startup, ModTek decorates the version number found in the bottom left corner of the main menu with `/W MODTEK`. If you don't see this something has gone wrong.

## Linux

The Modtek zip also contains the UnityDoorstop script `run.sh` and the hooking libraries to run the game with.

### Steam on Linux

> **Note**
> These instructions are based on the [Steam Guide for BepInEx](https://docs.bepinex.dev/master/articles/advanced/steam_interop.html).

Instead of running the `run.sh` script directly, you need to ask Steam to run it for you.

Right mouse click on the game in the Steam library -> `Properties...` -> `SET LAUNCH OPTIONS`.

Launch options for Linux:
> `./run.sh %command%`

### GOG on Linux

GOG installs into `battletech/game/`. Extract the zip file into the same directory as the `Battletech` executable.

Modify the `start.sh` script from GOG to execute the `run.sh` script supplied by ModTek/UnityDoorstop instead of `BattleTech`.

> ```
> #chmod +x "BattleTech"
> #./"BattleTech"
> ./run.sh
> ```

### Proton/Wine on Linux

Using Proton or Wine is also supported, make sure the `winhttp.dll` from UnityDoorstop is loaded by setting the override to `native, builtin`.

### mono crash on Linux

If a crash happens with the following error
> ```
> Receiving unhandled NULL exception
> #0  0x007f981f25cab9 in mini_get_debug_options
> #1  0x007f982693674c in init_mono
> ```

Add a file /etc/sysctl.d/01-disable-aslr.conf with contents
> kernel.randomize_va_space = 0

## macOS

> **Note**
> The installation instructions for macOS are similar to Linux, only differences are listed here.

The base installation folder is the `Contents/Resources` directory within the .app Application packages.
For a standard Steam installation that means the following path:
> `~/"Library/Application Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/"`

The easiest way to go to this folder is to go into Steam and select the gear icon->Manage->Browse local files. This will open up a Finder window. Right click on the Battletech app and select "Show Package Contents." From the next view, double click on Contents and then Resources. You are now in the correct folder. Copy the content of the ModTek download into the Resources folder.

### Steam on macOS

After copying the ModTek folders you will need to configure both the Steam launcher and set the correct file permissions for run.sh. To do both you need to first open a Terminal window in the resources directory. Either launch Terminal and CD to the Resources directory you navigated to above or if you have configured Terminal keyboard shortcut service, use that to open a Terminal Window in the Resources directory. First you will need to set the correct permissions for the run.sh script required to load ModTek via Doorstop. 

In Terminal type:
> `chmod +x run.sh`

This will set the correct run execute permissions for the run.sh script. 

Next you need to set the launch options for ModTek to load. Launch options for macOS need to contain the absolute path to the run.sh script.

In a terminal, run this from the same location where the run script is:
> `echo "\"$(pwd)/run.sh\" %command%"`

The launch options should then look something like this:
> `"/Users/ReplaceThisByYourUsername/Library/Application Support/Steam/steamapps/common/BATTLETECH/BattleTech.app/Contents/Resources/run.sh" %command%`

In Steam go to Battletech, select Gear->Properties and paste the output from running the command above in Terminal (copy and paste) into the Launch Options box. 

## Enabling or Disabling

> **Obsolete**
> Please report if this feature still works and/or if this is still in use.

ModTek 0.7.6 or higher can be enabled or disabled from within the in-game mods menu. If ModTek is enabled, the  "MODS ENABLED" check box will always be set to enabled. To disable ModTek look through the mod list until you find 'ModTek', and disable that 'mod'. Restart the game, and only the in-game mod-loader will be used. Repeat the process but enable the 'ModTek' mod to re-enable an external ModTek install.

> **Warning**
> You must restart the game to enable or disable an external ModTek!

## What is UnityDoorstop and what those files like winhttp.dll

[Unity DoorStop](https://github.com/NeighTools/UnityDoorstop) provides a set of files that trick Unity into loading ModTek, but without modifying the game files.
The old way was asking the user to run a `ModTekInjector.exe` manually, which modified the game so it loaded ModTek.
In cases of updates, the old way required the user to "verify" the game files and re-install mods, but using UnityDoorstop this is not necessary anymore.
