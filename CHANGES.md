# Release notes

All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## 2.0 Major Changes

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

## Before 2.0

see [ModTek releases](https://github.com/BattletechModders/ModTek/releases)
