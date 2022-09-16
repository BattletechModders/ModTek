using System.IO;

namespace ModTekPreloader
{
    internal static class SingleInstanceEnforcer
    {
        private const string LockFileRelativePath = "Mods/.modtek/.lock";
        private static FileStream LockFileStream;

        internal static void Enforce()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LockFileRelativePath));
            try
            {
                LockFileStream = new FileStream(
                    LockFileRelativePath,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.None, // this is only supported on windows
                    4096,
                    FileOptions.SequentialScan
                );
                // this should be supported on every platform
                LockFileStream.Lock(0, 0);
            }
            catch
            {
                Logger.Log($"Another BattleTech process is locking {LockFileRelativePath}");
                throw;
            }
        }
    }
}
