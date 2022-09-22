# Release notes

All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## 2.1 - CptMoore

For users:
- `ModTekInjector.exe` was replaced by [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop), removes the need to run any injector by the user.

For modders:
- ModTek now has a preloader that runs injectors to modify assemblies on-the-fly with Mono Cecil. See [Preloader](doc/PRELOADER.md).
- UnityDoorstop also makes it easy to override or add assemblies by putting them into `Mods/ModTek/AssembliesOverride/`.
- ModTek now has a switch to enable HarmonyX support with Harmony1 and 2 shims. See [Harmony12X](doc/HARMONY12X.md).

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
