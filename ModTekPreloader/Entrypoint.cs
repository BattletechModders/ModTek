using System;
using System.IO;
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
                Preloader.Run(Directory.GetCurrentDirectory());
                Logger.Log("Preloader finished");
            }
            catch (Exception e)
            {
                Logger.Log("Preloader failed: " + e);
            }
        }
    }
}