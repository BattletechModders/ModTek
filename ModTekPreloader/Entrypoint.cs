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
                Preloader.Run();
                Logger.Log("Preloader finished");
            }
            catch (Exception e)
            {
                Logger.Log("Preloader failed: " + e);
            }
        }
    }
}