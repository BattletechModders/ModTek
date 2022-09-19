using System;
using System.IO;
using System.Reflection;
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
                // this AppDomain allows us to unload all dlls used by the Preloader and Injectors
                var domain = AppDomain.CreateDomain("ModTekPreloader");
                try
                {
                    var preloader = (Preloader)domain.CreateInstanceAndUnwrap(
                        typeof(Preloader).Assembly.FullName,
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(Preloader).FullName
                    );
                    preloader.Run(new GameAssemblyLoader());
                }
                finally
                {
                    AppDomain.Unload(domain);
                }
            }
            catch (Exception e)
            {
                var message = "Exiting the game, preloader failed: " + e;
                try { Console.Error.WriteLine(message); } catch (Exception) { /* ignored */ }
                try { LogFatalError(message); } catch (Exception) { /* ignored */ }
                Environment.Exit(0);
            }
        }

        // used to preload the injected assemblies in the Game's AppDomain
        internal class GameAssemblyLoader : MarshalByRefObject
        {
            internal void LoadFile(string path)
            {
                Assembly.LoadFile(path);
            }
        }

        // Doesn't use the logger
        private static void LogFatalError(string message)
        {
            if (File.Exists(Paths.LogFile))
            {
                File.AppendAllText(Paths.LogFile, message);
            }
            else
            {
                Paths.CreateDirectoryForFile(Paths.LogFile);
                File.WriteAllText(Paths.LogFile, message);
            }
        }
    }
}