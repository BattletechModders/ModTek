# Release notes

All notable changes should be documented in this file.
Since v2, ModTek adheres to [Semantic Versioning](http://semver.org/).

## Known Issues

- Injected assemblies (dlls) are saved to disk during startup, virus scanners could trigger or block this process.
- Some mods expect the managed assemblies location to be in the `Managed` directory,
  however injected assemblies are now found under `Mods/.modtek/AssembliesInjected` or loaded directly into memory after injection.
- The HarmonyX feature works well, however some mods might rely on some buggy Harmony 1.2 behaviors that HarmonyX shims don't replicate.

## 4.2 - CptMoore

For users:
- Updated UnityDoorstop to latest version + still including our custom steamfix.
- Fixed run.sh to work with new steam wrappers

For modders:
- Moved all ModTek dlls (ModTek, HarmonyX, MonoMod, etc..) to `ModTek/lib`.
  - Requires to update the doorstop ini
  - Made run.sh depend on the doorstop.ini instead of having its own inline options
- Updated HarmonyX
  - New HarmonyX is based on a major rewrite of MonoMod, several bugs were encountered and fixed
  - Still providing an older version of HarmonyX in-case the new HarmonyX feels unstable
- Various Logging improvements and changes
  - Async logging is now highly optimized
    - reduction of 300+ ns to <100 ns spent on the caller thread (usually the unity main thread)
  - Reduced logging (format) options to increase performance
    - actual formatting times were reduces from 6-8 us to below 1 us
    - now nvme drives are the bottleneck (~3 us), not the formatting code
  - HBS' ILog.Flush() method is now supported and flushes OS buffers to disk

## 4.1 - kMiSSioN

For modders:
- Added a DebugDumpServer to allow triggering log dumps via HTTP calls, disabled by default

## 4.0 - CptMoore

For modders:
- Added example mods
- HarmonyX support can't be disabled anymore and is now the preferred way to patch during runtime
- HarmonyX logging is now fully integrated into ModTek

## 3.1 - CptMoore / kMiSSioN

For users:
- The Linux `run.sh` script was updated to fix a compatibility issue with Steam.

For modders:
- HarmonyX support is now enabled by default, see [Harmony12X](doc/HARMONY12X.md).
- Improved logging support via HBS Logger, see [Logging](doc/LOGGING.md).
- Added a `Assembly` CustomResource type, allows resolving third-party .NET dlls without writing resolve handlers in mods.

## 3.0 - CptMoore

> **Note**
> Previously known as 2.1, but due to some mods and users having trouble, it is now called 3.0 .

For users:
- `ModTekInjector.exe` was replaced by [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop), removes the need to run any injector by the user.

For modders:
- ModTek now has a preloader that runs injectors to modify assemblies on-the-fly with Mono Cecil. See [Preloader](doc/PRELOADER.md).
- UnityDoorstop also makes it easy to override or add assemblies by putting them into `Mods/ModTek/AssembliesOverride/`.
- ModTek has support for HarmonyX with shims for Harmony 1.2 and 2. Disabled by default. See [Harmony12X](doc/HARMONY12X.md).
- The experimental profiler was removed, it wasn't very good and its heavy use of Harmony made it incompatible with the HarmonyX support.

## 2.0 - CptMoore

For users:
- Moved Wiki content to main repository.

For modders:
- Support for merging DLC content. Overall improved support for DLC content.
- Dropped support for BTRL removals.
- StreamingAssets resources are now better supported, only need to use the correct filename to merge. No need to mirror the path in StreamingAssets or supply a type.
- Many more configuration options and all of them described in `ModTek/config.defaults.json` (once the game started at least once with ModTek enabled).
- Mods are loaded recursively from subdirectories by default, e.g. `Mods/MyWorkspace/MyMods/mod.json`.
- Integrated the FYLS mod into ModTek, central log found in `.modtek/battletech_log.txt`. Improved HBS logging format in general.
- Internal representation of the games manifest is dumped to `.modtek/Manifest.csv`, that includes CustomResources.
- (Experimental) A simple profiler that may help to determine causes for stuttering. The profiler is disabled by default.

## 0.8.0 - IceRaptor

- Adds CustomTags and CustomTagSets as an importable type.
- Tag and TagSets can be defined in JSON and imported directly.
- See https://github.com/battletechmodders/modtek/#custom-tags-and-tagsets for details.

## 0.7.7 - KMiSSioN / IceRaptor

- Load time optimizations (minor)
- Changes various logging to only run if Config.EnableDebugLogging is set to true. Slightly improves load time.
- Read new debug logging config setting
- Change config to read from modtek directory, instead of .modtek
- Invalidate the addendum cache during RefreshTypedEntries when ModTek adds or removes content
- Debug log gate another profiling log.
- Appears to correct issues with ShadowHawk DLC that occurred in 0.7.7.5 again
- Added support for additional soundbanks postload processing
- Fixes issue #172; failed loading under 1.9.1 when ShadowHawk DLC is enabled.
- Potentially fixes Resource locator patch by forcing its manifest to be loaded early
- Note there are two versions provided. The base packages use the 1.2.0.1 0Harmony.dll.
  The -nofastinvoke packages include a modified Harmony.dll provided by @m22spencer that has load-time improvements. This has proven stable on RogueTech, but users are encouraged to test.
- Added support for ShipUpgradeCategory (thanks to Jamie Wolf, Keeper of the Void)
- Added ModTekAutoInjector mod for build-in modloader. Unpack ModTekAutoInjector to build-in mod loader mod folder and you do not need to run ModTekInjector.exe. Injection will be performed at first run.
- BT 1.9 compatibility

## 0.7.6 - KMiSSioN

- Now RTPC ids for controlling volume of external sound banks can me set in sound banks definitions
- absent UI icons not added to prewarm
- Icons loaded from external files (SVGAsset manifest entry type) can now be used in AmmoCategory and WeaponCategory definitions
- Added support SVGAsset manifest entry type loading from .svg files.
- Dynamic enums can use ID now
- Added support for SoundBanksDef.
- Dynamic enums loading improved. Now if number of mods updating same enum type consolidation processed correctly.
- Now you do not need to keep ID unique in dynamic enums, only name. ID will be calculated automatically.
- ModTek.allModDefs now public, other mods can list loaded mods.
- Dynamic enums now loading before adding to mod's files to DB.
- On change mods enable/disable settings ModTek cache clearing
- Fixed bug with occasionally enabling/disabling mods.

## Earlier versions

Listed below are the maintainers that tagged earlier versions.
For the full list of contributors and commit history
visit [ModTek](https://github.com/BattletechModders/ModTek/) at GitHub.

### CMiSSioN / KMiSSioN
- 0.7.4 - 0.7.5

### mpstark
Original author and maintainer.
- 0.1.0 - 0.2.3
- 0.4.2 - 0.7.3

### Morphyum
- 0.4.1

### janxious
- 0.3.0 - 0.4.0
