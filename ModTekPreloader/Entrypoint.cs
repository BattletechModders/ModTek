using System;
using ModTekPreloader;

namespace Doorstop
{
    class Entrypoint
    {
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