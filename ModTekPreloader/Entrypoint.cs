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
                Logger.Reset();
                Preloader.Run(Directory.GetCurrentDirectory());
            }
            catch (Exception e)
            {
                Logger.Log("Preloader failed: "+ e);
            }
        }
    }
}