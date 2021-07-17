# Release notes

All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## 2.0 Major Changes

For users:
- Moved Wiki content to main repository.

For modders:
- Support for merging DLC content. Overall "improved" support for DLC content, maybe fixes issues with online shops.
- Dropped support for BTRL removals and CustomResources merging.
- StreamingAssets resources are now better supported, only need to use the correct filename to merge. No need to mirror the path in StreamingAssets or supply a type for merges outside.
- CSVs are now merged by default when under StreamingAssets (before it was replacing and not appending). Behavior can be changed with ImplicitManifestShouldAppendText in ModTek config.
- Integrated the FYLS mod into ModTek, central log found in `.modtek/battletech_log.txt`. Improved HBS logging format in general.

More details found below.

## 2.0.next

- Renamed ContentPackMerges/ to ContentPackAssets/ , support same behavior as StreamingAssets just that you still have to provide a Type and the related content is only loaded when the dlc content is owned.
- Improved HBS logging format.

## 2.0.3

- The default merging behavior of the implicit manifest can be changed. Allows to revert merging behavior for CSVs back to pre-ModTek v2.
  See ModTek config: ImplicitManifestShouldMergeJSON and ImplicitManifestShouldAppendText.

Bug Fixes:
- Fixes for types being wrongly/not derived derived and producing exceptions.

## 2.0.2

- Introduced CI using github, see "actions" tab if you have the rights. All branches are build and pushes to master are publishes as "latest". Pushed tags are build and draft release are created with the assets containing the versioned build.

Bug Fixes:
- Properly catch mods throwing exceptions in FinishedLoading.
- Fix AssetBundleName being ignored at some places.
- Fixed career and campaign loading issues related to GetAddendumByName.

## 2.0.1

- Added ability to block mods by name.
- Integrated FYLS into ModTek. All logging in BattleTech (core or mods) are coming up in .modtek/battletech_log.txt

Bug Fixes:
- OnLoadedWithText patch was given higher priority to keep compatibility with KMission's CustomBundle mods.

## 2.0.0

- Restructured ModTek into "Features" and modularized code. Still lots of static classes with state and cross-calling between classes, but better than before.
- Modernized csproj in use, now using the latest and greatest from MS. (imports and listing of all files went away) Fully compatible now with the dotnet build suite.
- GameTips and DebugSettings are now special cases for StreamingAssets instead of custom resources.

## Before 2.0

see [ModTek releases](https://github.com/BattletechModders/ModTek/releases)
