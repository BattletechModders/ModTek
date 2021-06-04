This will be a guide for writing a fairly simple mod -- making the Leopard have a 300 ton drop-limit while playing the campaign/simgame.

## Getting Started

The easiest way to change how the game works is to see if you can do it by editing the game's JSON and writing a ModTek JSON mod -- some examples are [here](Writing-ModTek-JSON-mods.md). You should only spend your time and effort if that time and effort is required to get your project working. HBS has provided a lot of tweaks to the way that the game works in JSON files and it's not uncommon that they've got something already in place. In this case however, there are none used by the game so we'll just have to make it.

### Tools

This tutorial/walk-through uses:

* BattleTech itself
* [Visual Studio Community 2019](https://www.visualstudio.com/downloads/) to write and compile your mod's `.dll`
* [dnSpy](https://github.com/0xd4d/dnSpy) to decompile game assembly back to C#
* [Harmony](https://github.com/pardeike/Harmony) to patch methods at runtime

## Do Your Research

In order to modify the way that something works, you should probably learn how it works to begin with. Use dnSpy's search and analyze functionalities to get familiar with the code. Note the pulldowns in the search feature, where you can filter for certain members, or numbers and strings. A more detailed primer for navigating BattleTech's code with dnSpy will probably come.

In our case, we want to simply add the condition that you cannot launch a mission while overweight, which pretty closely matches the code for not being able to launch without a Mechwarrior in a 'Mech. By digging around, I found `BattleTech.UI.LanceConfiguratorPanel.ValidateLance()`, which is the function that is called to see if the configured Lance is valid or not.

## Setting Your Project Up

Create a new project and solution with Visual Studio. You should target .NET 4.7, as this is what the game is using.

On the right, make sure that you add references to the `0Harmony.dll` and `Assembly-CSharp.dll`. If you will be using the settings from your `mod.json`, you will also need `Newtonsoft.Json.dll`. 

It's a hassle to change the other references like System and such to match the installed game and unless you're doing something special, you can skip changing the references. You do not need to reference ModTek.

## Actually Writing Your Mod

Now that you understand how the functionality works, you can change it. In our case, we want `ValidateLance` to return false when the lance is overweight, as well as fill in the same values that the function already does (i.e. we want to have exactly the same side effects as if the code was written in the method itself). It would be relatively easy to just hop into changing it in dnSpy and recompiling the method -- but we can't do that in this case, since we want to have a seperate `.dll` that does it at runtime.

That's why we're using [Harmony](https://github.com/pardeike/Harmony) -- it allows you to "hook" onto methods before and after they are called, as well as to directly modify the executed IL code with a transpiler. The 'hooks' before are called Prefixes; they can modify the parameters passed into the function, as well as actually prevent the original code from being called, and the hooks after are called Postfixes; which can modify what the function returns. You can learn more about Harmony from looking at it's [wiki](https://github.com/pardeike/Harmony/wiki) and looking through other people's Harmony-based mods.

Since `ValidateLance` doesn't have parameters, we still want the original code to execute, and we want to change `ValidateLance`'s return value, we'll use a postfix patch, which something like this:

```csharp
[HarmonyPatch(typeof(LanceConfiguratorPanel), "ValidateLance")]
public static class LanceConfiguratorPanel_ValidateLance_Patch
{
    public static void Postfix(LanceConfiguratorPanel __instance, ref bool __result)
    {
    }
}
```

Right now, it doesn't do anything and it won't even get called. You'll notice that it's static class with a `Postfix` method, that it's got an annotation that has the type (we're `using BattleTech.UI;` at the top of the file) and method name passed as a (magic) string. This is to tell Harmony which method specifically that we want to patch (if it is overloaded, you'll need to provide an array of parameter types too!). But first, in order for this patch to even get setup, we'll need to setup Harmony to read these annotations.

ModTek will call `Init(void)`, `Init(string, string)` or you can setup a custom entry point in your `mod.json` file. We'll just use the default entry point with the two string parameters, they'll be useful to us later. So we'll setup a 'main' static class that contains an `Init(string, string)`.

```csharp
public static class DropLimit
{
    public static void Init(string directory, string settingsJSON)
    {
        var harmony = HarmonyInstance.Create("io.github.mpstark.DropLimit");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
```

This will instantiate a HarmonyInstance with your unique identifier (Harmony recommends the reverse domain notation, but any unique string will work), as well as search your entire assembly for classes with annotations like the one we setup on our patch class. Now, if we compiled our code and dragged our `.dll` into our mod folder with our `mod.json`, our blank method would be called every time after `ValidateLance` is called.

Let's make our patch do something. First, take a look at the parameters that I setup. Both are special parameters given by Harmony -- the first is to get the object that this particular call is for, sort of a `this` for Harmony. The second is a `ref bool` type, because the function returns a `bool` value and we want to be able to change it.

Returning to dnSpy, we need to figure out exactly what we need to do to emulate what the method does when it detects an error -- since it doesn't *just* return false when it detects on error, it has other side-effects. Namely, it sets `lanceErrorText` to the error, and it also passes it to the `headerWidget` object, along with some other infomation. In order to do this correctly, we have to do all three things.

Once we start implementing this, we run immediately into the issue of accessing non-public fields. Because we have merely have a reference to `__instance`, we can only do the normal public things, and what we want to do is.. private.

Harmony has a couple utilities for this. The first is simply using three underscores in front of the name in the patch parameters to get at the variable. You can also use the Harmony `Traverse` class.

```csharp
public static void Postfix(LanceConfiguratorPanel __instance, ref bool __result, LanceLoadoutSlot[] ___loadoutSlots, LanceHeaderWidget ___headerWidget, string ___lanceErrorText)  
// using triple underscores for parameters now automatically accesses private fields using Harmony.  _myFieldName would be accessed with 4 underscores.
{
    float lanceTonnage = 0;

    var mechs = new List<MechDef>();
    for (var i = 0; i < __instance.maxUnits; i++)
    {
        var lanceLoadoutSlot = ___loadoutSlots[i];

        if (lanceLoadoutSlot.SelectedMech == null) continue;

        mechs.Add(lanceLoadoutSlot.SelectedMech.MechDef);
        lanceTonnage += lanceLoadoutSlot.SelectedMech.MechDef.Chassis.Tonnage;
    }

    if (lanceTonnage <= 300) return;

    __instance.lanceValid = false;

    ___headerWidget.RefreshLanceInfo(__instance.lanceValid, "Lance cannot exceed tonnage limit", mechs);

    ___lanceErrorText = "Lance cannot exceed tonnage limit\n";

    __result = __instance.lanceValid;
}

You'll notice that we had to do some extra stuff to satisfy the side effects of the original method, namely make a list of MechDefs to pass to `RefreshLanceInfo`. Compile, drag the compiled result to our mod folder, run the game and it works!

### Making it better

Remember when I said that ModTek could pass you the settings json from the `mod.json` file? Let's use it! The easiest way is to create a settings class with some default values. We don't have to setup a constructor because one is generated for us for such a simple class.

```csharp
internal class ModSettings
{
    public float MaxTonnage = 300;
    public bool OnlyInSimGame = true;
}
```

```csharp
internal static ModSettings Settings = new ModSettings();
public static void Init(string directory, string settingsJSON)
{
    var harmony = HarmonyInstance.Create("io.github.mpstark.DropLimit");
    harmony.PatchAll(Assembly.GetExecutingAssembly());

    // read settings
    try
    {
        Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON);
    }
    catch (Exception)
    {
        Settings = new ModSettings();
    }
}
```

This will use Newtonsoft.Json to create a new settings object for our mod, which will be stored in a our static class that holds `Init`. If the settings json has problems or doesn't exist, then we'll just use the default settings. Note that this sort of error handling is *fast and loose* and you shouldn't actually do it.

Now it's easy to change our patch to use these settings.

```csharp
if (DropLimit.Settings.OnlyInSimGame && !__instance.IsSimGame)
    return;
// ...
if (lanceTonnage <= DropLimit.Settings.MaxTonnage)
    return;
```