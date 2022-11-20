using System;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;

// ReSharper disable once CheckNamespace
namespace Doorstop;

// ReSharper disable once UnusedMember.Global
class Entrypoint
{
    // ReSharper disable once UnusedMember.Global
    public static void Start()
    {
        try
        {
            Preloader.Run();
        }
        catch (Exception e)
        {
            var message = "Exiting the game, preloader failed: " + e;
            try { Console.Error.WriteLine(message); } catch { /* ignored */ }
            try { Logger.Main.Log(message); } catch { /* ignored */ }
            Environment.Exit(0);
        }
    }
}