using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ModTek.Common.Utils;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Loader;

internal static class SingleInstanceEnforcer
{
    private static FileStream LockFileStream;
    private static string LogPrefix => $"SingleInstanceEnforcer [pid={Process.GetCurrentProcess().Id}]:";

    internal static void Enforce()
    {
        FileUtils.CreateDirectoryForFile(Paths.LockFile);
        try
        {
            Logger.Main.Log($"{LogPrefix} Locking");

            Retry(
                () =>
                {
                    LockFileStream = new FileStream(
                        Paths.LockFile,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.None, // this is only supported on windows
                        4096,
                        FileOptions.SequentialScan
                    );
                },
                () => Logger.Main.Log($"{LogPrefix} Can't open or create, retrying")
            );

            // this should be supported on every platform
            Retry(
                () => LockFileStream.Lock(0, 0),
                () => Logger.Main.Log($"{LogPrefix} Can't lock, retrying")
            );

            Logger.Main.Log($"{LogPrefix} Locked");

            // unlocks the lock file hopefully a bit earlier to avoid steam re-launch issues
            AppDomain.CurrentDomain.ProcessExit += (s, args) =>
            {
                Logger.Main.Log($"{LogPrefix} Unlocking");
                LockFileStream.Close();
                LockFileStream = null;
                Logger.Main.Log($"{LogPrefix} Unlocked");
            };
        }
        catch
        {
            Logger.Main.Log($"{LogPrefix} Another BattleTech process is locking {Paths.LockFile}");
            throw;
        }
    }

    private const int RETRIES = 10;
    private static readonly TimeSpan SLEEP_TIMEOUT = TimeSpan.FromSeconds(1);
    private static void Retry(Action action, Action logRetry)
    {
        for (var i = 0; i <= RETRIES; i++)
        {
            try
            {
                action();
                break;
            }
            catch
            {
                if (i == RETRIES)
                {
                    throw;
                }
                logRetry();
                Thread.Sleep(SLEEP_TIMEOUT);
            }
        }
    }
}