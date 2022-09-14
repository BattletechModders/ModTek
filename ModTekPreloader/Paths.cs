using System;
using System.IO;

namespace ModTekPreloader
{
    internal class Paths
    {
        internal const string GAME_DLL_FILE_NAME = "Assembly-CSharp.dll";

        private const string ENV_DOORSTOP_MANAGED_FOLDER_DIR = "DOORSTOP_MANAGED_FOLDER_DIR";

        internal readonly string managedDirectory;
        internal readonly string gameDLLPath;
        internal readonly string modsDirectory;
        internal readonly string injectorsDirectory;
        internal readonly string assembliesInjectedDirectory;

        internal Paths()
        {
            managedDirectory = Environment.GetEnvironmentVariable(ENV_DOORSTOP_MANAGED_FOLDER_DIR)
                ?? throw new Exception($"Can't find {ENV_DOORSTOP_MANAGED_FOLDER_DIR}");
            gameDLLPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME);

            var gameExeDirectory = Directory.GetCurrentDirectory();
            modsDirectory = Path.Combine(gameExeDirectory, "Mods");
            injectorsDirectory = Path.Combine(modsDirectory, "ModTek", "Injectors");
            assembliesInjectedDirectory = Path.Combine(modsDirectory, ".modtek", "AssembliesInjected");
            ExistsOrThrow(managedDirectory, gameDLLPath, modsDirectory);
        }

        private static void ExistsOrThrow(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    throw new Exception($"Can't find {path}");
                }
            }
        }
    }
}
