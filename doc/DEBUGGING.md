# Debugging

Debugging allows to follow the runtime behavior and in-memory data of BattleTech and its mods.

Only the .NET (C#) part of the game can be debugged.

You can't debug the dynamic patching part of Harmony,
meaning you can only debug un-patched methods or any methods that are called from a pre- or postfix patch.
Any breakpoint set in a method that gets Harmony patches, will not break.

Steps to get a working debugger setup:

1. download [dnSpyEx](https://github.com/dnSpyEx/dnSpy)
2. download the mono dll from dnSpyEx mono dlls matching your bt ( 2018.4.2 ) and put it into `BATTLETECH/MonoBleedingEdge/EmbedRuntime`
   1. TODO prebuild variants are not available anymore (Doorstop works mostly even with non-debug dlls, just enable Doorstops debugging)
   2. The file in question would be called `mono-2.0-bdwgc.dll`
3. start dnSpyEx and add all dlls from `BATTLETECH/BattleTech_Data/Managed` and `Mods/*/*.dll` to its project window
4. find the spot you want to debug, place a breakpoint
5. if on steam, prepare the steamappid file
   1. create a file as `BATTLETECH/steam_appid.txt` with the contents: `637090`
6. start the game
7. connect dnSpyEx to Unity (default port etc..)
8. your breakpoint should be hit as long as no mods patch the method with your breakpoint (see `harmony_summary.log`)
