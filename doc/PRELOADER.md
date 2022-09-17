# Preloader and Injectors

> **Note**
> TODO finalize

- Preloader uses UnityDoorstop to run itself, UnityDoorstop uses native libraries to load so early that the game didn't even load any .NET assemblies.
  That way injectors now can modify the games .NET assemblies as part of the loading process, instead of the user having to do it.
- Runs injectors, e.g. ModTekInjector injects ModTek.
- UnityDoorstop also makes it easy to override or add assemblies. Just put them into `Mods/ModTek/AssembliesOverride`.
- UnityDoorstop also sets env variables to find the managed directory.
- Load order of assemblies
  -  `BATTLETECH/Mods/.modtek/AssembliesInjected/` (directory updated during Preloader run, each assembly in here was already force loaded during the run)
  -  `BATTLETECH/Mods/ModTek/AssembliesOverride/` (doorstop settings to allow updating libs from the managed folder)
  -  `BATTLETECH/Mods/ModTek/` (doorstop adds this since it loads Preloader from here)
  -  `BATTLETECH/BattleTech_Data/Managed` (or the equivalent directory based on platform, should never be modified)

## Injectors

> **Note**
> TODO finalize

- See [ModTekInjector](https://github.com/BattletechModders/ModTek/blob/master/ModTekInjector/ModTekInjector.csproj)
  or [RogueTechPerfFixesInjector](https://github.com/BattletechModders/RogueTechPerfFixes/blob/master/RogueTechPerfFixesInjector/RogueTechPerfFixesInjector.csproj)
  on how an injector works.
- Only assemblies resolved as a AssemblyDefinition will be loaded into the game, make sure to only use the resolver interface in the Inject method.
- Injectors are loaded and run in the order of their names from `Mods\ModTek\Injectors\`.
- The preloader searches for a class named `Injector` with a `public static void Inject` method, that then will be called with a parameter of type `Mono.Cecil.IAssemblyResolver`.
  > ```
  > internal static class Injector
  > {
  >   public static void Inject(Mono.Cecil.IAssemblyResolver resolver)
  >   {
  >     var game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
  >   }
  > }
  > ```
- Injectors run in their own AppDomain. All directly loaded dlls (via Assembly.Load or due to reference in Assembly) during the injection phase will be lost.
- Console output is redirected into `Mods\.modtek\ModTekPreloader.log`, use `Console.WriteLine` or `Console.Error.WriteLine` instead of writing a logger.
- Modified assemblies (that were resolved earlier via the resolver) are then written to and loaded from `Mods\.modtek\AssembliesInjected\`

## Publicized Assemblies

> **Warning**
> The publicized assemblies feature is still being developed.
> Inheriting from publicized classes is prone to crashes and produces incompatibility issues with other mods.

> **Note**
> TODO finalize

- Makes all classes, methods, properties and fields public (with some exceptions) for a select few assemblies.
- Those assemblies are only for compiling against, these assemblies are not loaded into the game!
- Avoids the need to write reflection tools or wrappers to access private stuff, leads to cleaner and faster code.
- Requires that dlls using publicized assemblies are marked as unsafe, to disable runtime access checks. Put into your csproj:
  > ```<AllowUnsafeBlocks>true</AllowUnsafeBlocks>```
- Make your mod reference `Mods\.modtek\AssembliesPublicized\`:
  > ```<AssemblySearchPaths>$(BattleTechGameDir)\Mods\.modtek\AssembliesPublicized\;$(BattleTechGameDir)\BattleTech_Data\Managed\</AssemblySearchPaths>```
