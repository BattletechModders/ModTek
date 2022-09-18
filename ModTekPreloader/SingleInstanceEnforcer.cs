using System.IO;

namespace ModTekPreloader
{
    internal static class SingleInstanceEnforcer
    {
        private static FileStream LockFileStream;

        internal static void Enforce()
        {
            Paths.CreateDirectoryForFile(Paths.LockFile);
            try
            {
                LockFileStream = new FileStream(
                    Paths.LockFile,
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
                Logger.Log($"Another BattleTech process is locking {Paths.LockFile}");
                throw;
            }
        }
    }
}
