# Release notes
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## 2.0.2

Features:
- Introduced CI using github, see "actions" tab if you have the rights. All branches are build and pushes to master are publishes as "latest". Pushed tags are build and draft release are created wit the assets containing the versioned build.

Bug Fixes:
- Properly catch mods throwing exceptions in FinishedLoading.
- Fix AssetBundleName being ignored at some places.
- Fixed career and campaign loading issues related to GetAddendumByName.

## 2.0.1

Features:
- Added ability to block mods by name.
- Integrated FYLS into ModTek. All logging in BattleTech (core or mods) are coming up in .modtek/battletech_log.txt

Bug Fixes:
- OnLoadedWithText patch was given higher priority to keep compatibility with KMission's CustomBundle mods.

## 2.0.0

For users and modders:
- Support for merging DLC content. Overall "improved" support for DLC content, maybe fixes issues with online shops.
- Dropped support for BTRL removals and CustomResources merging.
- StreamingAssets resources are now better supported, only need to use the correct filename to merge. No need to mirror the path in StreamingAssets or supply a type for merges outside. GameTips and DebugSettings are now special cases for StreamingAssets instead of custom resources.
- Moved Wiki content to main repo.

For ModTek devs:
- Restructured ModTek into "Features" and modularized code. Still lots of static classes with state and cross-calling between classes, but better than before.
- Modernized csproj in use, now using the latest and greatest from MS. (imports and listing of all files went away) Fully compatible now with the dotnet build suite.

## Before 2.0

see [ModTek releases](https://github.com/BattletechModders/ModTek/releases)
