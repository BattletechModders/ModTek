using System.Collections.Generic;
using System.Threading;

namespace ModTek.Features.Logging
{
    // TODO integrate all Loggers: BTLogger, MTLogger and RTLog!
    internal static class RLog
    {
        private static Dictionary<RTLogFileType, RTLogFile> logs = new();

        //private static string m_assemblyFile;
        public static string BaseDirectory;
        public static readonly int flushBufferLength = 16 * 1024;
        public static bool flushThreadActive = true;
        public static Thread flushThread = new(flushThreadProc);

        public static void flushThreadProc()
        {
            while (flushThreadActive)
            {
                Thread.Sleep(30 * 1000);
                //RLog.LogWrite("Log flushing\n");
                flush();
            }
        }

        public static void flush()
        {
            foreach (var log in logs)
            {
                log.Value.flush();
            }
        }

        public static void LogWrite(string line, bool isCritical = false)
        {
            if (logs.ContainsKey(RTLogFileType.Main) == false)
            {
                return;
            }

            logs[RTLogFileType.Main].W(line, isCritical);
        }

        public static RTLogFile M => logs[RTLogFileType.Main];

        public static void InitLog(string baseDir, bool isDebug)
        {
            //LogFile file = new LogFile("CAC_main_log.txt", CustomAmmoCategories.Settings.debugLog);
            BaseDirectory = baseDir;
            logs.Add(RTLogFileType.Main, new RTLogFile("ModTek_runtime_log.txt", isDebug));
            //Log.logs.Add(LogFileType.Main, null);
            flushThread.Start();
        }
    }
}
