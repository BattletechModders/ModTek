using System;
using ModTekPreloader;

// ReSharper disable once CheckNamespace
namespace Doorstop
{
    // ReSharper disable once UnusedMember.Global
    class Entrypoint
    {
        // ReSharper disable once UnusedMember.Global
        public static void Start()
        {
            try
            {
                Logger.Setup();
                Logger.Log("Preloader starting");
                Paths.Print();
                SingleInstanceEnforcer.Enforce();
                Preloader.Run();
                Logger.Log("Preloader finished");
            }
            catch (Exception e)
            {
                Logger.Log("Exiting the game, preloader failed: " + e);
                Environment.Exit(0);
            }
        }
    }
}