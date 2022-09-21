using System;
using System.IO;
using ModTekPreloader;
using ModTekPreloader.Loader;

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
                var preloader = new Preloader();
                preloader.Run();
            }
            catch (Exception e)
            {
                var message = "Exiting the game, preloader failed: " + e;
                try { Console.Error.WriteLine(message); } catch { /* ignored */ }
                try { LogFatalError(message); } catch { /* ignored */ }
                Environment.Exit(0);
            }
        }

        // Doesn't use the logger
        private static void LogFatalError(string message)
        {
            if (File.Exists(Paths.LogFile))
            {
                File.AppendAllText(Paths.LogFile, message + Environment.NewLine);
            }
            else
            {
                Paths.CreateDirectoryForFile(Paths.LogFile);
                File.WriteAllText(Paths.LogFile, message + Environment.NewLine);
            }
        }
    }
}