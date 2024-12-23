using System;
using System.Linq;
using System.Reflection;
using ModTek.Preloader.Loader;
using Logger = ModTek.Preloader.Logger;

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
            if (e is BadImageFormatException e3)
            {
                message += "\n" + e3.FileName + " " + e3.FusionLog;
            }
            if (e is ReflectionTypeLoadException e2)
            {
                message += Environment.NewLine + e2.Message;
                if (e2.Types != null)
                {
                    try
                    {
                        message += string.Join(Environment.NewLine, e2.Types.Select(t => t.FullName));
                    }
                    catch (Exception ex)
                    {
                        message += Environment.NewLine + ex;
                    }
                }
                message += string.Join(Environment.NewLine, e2.LoaderExceptions.Select(x => x.ToString()));
            }
            try { Console.Error.WriteLine(message); } catch { /* ignored */ }
            try { Logger.Main.Log(message); } catch { /* ignored */ }
            Environment.Exit(0);
        }
    }
}