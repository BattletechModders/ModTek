# Developing ModTek

This page lists some common knowledge useful if you're extending or improving ModTek. 

# Suggested Tools and Files

Visual Studio 2022 Community
- visual studio 2022 community is for free
- newer visual studios and newer C# versions can be used, as long as they can target the old .NET Framework 4.7.2 used by the game.
- also the latest csproj format is supported, ModTek itself uses it and it works fine

Harmony
- dll that helps with in-memory patching of .NET code
- provides standards on how to hook into methods (prefixes, postfixes, transpilers) to allow a high degree of compatibility between mods
- ModTek itself also relies on harmony to patch the game' code

dnSpy
- tool to decompile and debug .NET dlls that come with BattleTech and Mods
- setup by removing all dlls from the aseembly explorer list and only add all dlls found in BattleTech_DatA/Managed/*.dll and Mods/**/*.dll .
- debugging requires a debug enabled mono dll replacing the existing mono dll of the game. needs to be the correct mono version.
- debugging works great, however harmony transpiled or prefixed skipped methods can't be debugged, this requires writing harmony patches prefix/postfixing and logging.

BTDebug Mod
- is a ModTek mod
- allows to navigate and see the UI unity tree structure
- also allows to modify UI elements, e.g. position, text, active state, etc..

GitHub Workflow
- ModTek uses github workflow as a CI
- every commit is built (no automated testing though!)
- commits on master are release as "latest" in github
- tags are released as their version number on github

publicized assemblies
- ModTek doesn't use publicized assemblies, but other mods can use them and are still compatible
- one can convert the existing game' dlls to publicized assemblies
- publicized assemblies means that all fields and classes visibility are changed to public
- avoids having to use Traverse from harmony
- requires the "unsafe" setting for the project
- has some issues when inheriting classes

# Build ModTek

After checking out the project, you must update a configuration file with the full path to your BattleTech Game directory.
Copy the file from `CHANGEME.Directory.Build.props` to `Directory.Build.props`.
Open `Directory.Build.props` in the editor of your choice, and replace the path value for `BattleTechGameDir` with the full path to your BattleTech Game.
Close and save. `Directory.Build.props` is excluded in `.gitignore` so you changes will not affect other developers, only you.

ℹ️Linux users should note that `Directory.Build.props` is case-sensitive.
If you find the project won't compile for you, make sure the case is correct.

Once you can updated the configuration, open the VS solution and restore NuGet dependencies.
You should be able to build ModTek once these steps are complete.

# Releasing ModTek

See GitHub Workflow above.
