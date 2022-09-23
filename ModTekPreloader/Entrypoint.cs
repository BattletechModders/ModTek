using System;
using System.IO;
using System.Reflection;
using ModTekPreloader;
using ModTekPreloader.Harmony12X;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;

// ReSharper disable once CheckNamespace
namespace Doorstop
{
    // ReSharper disable once UnusedMember.Global
    class Entrypoint
    {
        public const string AppDomainNameUnity = "Unity Root Domain";
        public const string AppDomainNamePreloader = "ModTekPreloader Domain";

        private static bool resetLog;
        // ReSharper disable once UnusedMember.Global
        public static void Start()
        {
            resetLog = true;
            try
            {
                // this AppDomain allows us to unload all dlls used by the Preloader and Injectors
                var domain = AppDomain.CreateDomain(AppDomainNamePreloader);
                Preloader preloader;
                try
                {
                    preloader = (Preloader)domain.CreateInstanceAndUnwrap(
                        typeof(Preloader).Assembly.FullName,
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(Preloader).FullName
                    );
                    resetLog = false;
                    preloader.PrepareInjection();
                    preloader.RunInjectors(new GameAssemblyLoader());
                }
                catch
                {
                    AppDomain.Unload(domain);
                    throw;
                }

                if (preloader.ShouldRegisterShimInjectorPatches)
                {
                    ShimInjectorPatches.Register(preloader);
                }
                else
                {
                    AppDomain.Unload(domain);
                }
            }
            catch (Exception e)
            {
                var message = "Exiting the game, preloader failed: " + e;
                try { Console.Error.WriteLine(message); } catch { /* ignored */ }
                try { LogFatalError(message); } catch { /* ignored */ }
                Environment.Exit(0);
            }
        }

        // used to preload assemblies in the Game's AppDomain
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
            if (!resetLog && File.Exists(Paths.LogFile))
            {
                File.AppendAllText(Paths.LogFile, message + Environment.NewLine);
            }
            else
            {
                // first access in an AppDomain triggers a log rotation
                Logger.Log(message);
            }
        }
    }
}