using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ModTek.RuntimeLog
{
    public enum LogFileType
    {
        Main
    }

    public class LogFile
    {
        private string m_logfile;
        private Mutex mutex;
        private StringBuilder m_cache = null;
        private StreamWriter m_fs = null;
        private bool enabled;

        public LogFile(string name, bool enabled)
        {
            try
            {
                mutex = new Mutex();
                this.enabled = enabled;
                m_cache = new StringBuilder();
                m_logfile = Path.Combine(RLog.BaseDirectory, name);
                var loginfo = new FileInfo(m_logfile);
                //if (loginfo.Exists && (loginfo.Length > 10 * 1024 * 1024))
                //{
                File.Delete(m_logfile);
                //}
                m_fs = new StreamWriter(m_logfile, false);
                m_fs.AutoFlush = true;
            }
            catch (Exception)
            {
            }
        }

        public void flush()
        {
            if (mutex.WaitOne(1000))
            {
                m_fs.Write(m_cache.ToString());
                m_fs.Flush();
                m_cache.Length = 0;
                mutex.ReleaseMutex();
            }
        }

        public void W(string line, bool isCritical = false)
        {
            if (enabled || isCritical)
            {
                if (mutex.WaitOne(1000))
                {
                    m_cache.Append(line);
                    mutex.ReleaseMutex();
                }

                if (isCritical)
                {
                    flush();
                }

                ;
                if (m_logfile.Length > RLog.flushBufferLength)
                {
                    flush();
                }

                ;
            }
        }

        public void WL(string line, bool isCritical = false)
        {
            line += "\n";
            W(line, isCritical);
        }

        public void W(int initiation, string line, bool isCritical = false)
        {
            var init = new string(' ', initiation);
            line = init + line;
            W(line, isCritical);
        }

        public void WL(int initiation, string line, bool isCritical = false)
        {
            var init = new string(' ', initiation);
            line = init + line;
            WL(line, isCritical);
        }

        public void TW(int initiation, string line, bool isCritical = false)
        {
            var init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            W(line, isCritical);
        }

        public void TWL(int initiation, string line, bool isCritical = false)
        {
            var init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            WL(line, isCritical);
        }
    }

    internal static class RLog
    {
        private static Dictionary<LogFileType, LogFile> logs = new();

        //private static string m_assemblyFile;
        public static string BaseDirectory;
        public static readonly int flushBufferLength = 16 * 1024;
        public static bool flushThreadActive = true;
        public static Thread flushThread = new(flushThreadProc);

        public static void flushThreadProc()
        {
            while (flushThreadActive == true)
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
            if (logs.ContainsKey(LogFileType.Main) == false)
            {
                return;
            }

            logs[LogFileType.Main].W(line, isCritical);
        }

        public static LogFile M => logs[LogFileType.Main];

        public static void InitLog(string baseDir, bool isDebug)
        {
            //LogFile file = new LogFile("CAC_main_log.txt", CustomAmmoCategories.Settings.debugLog);
            BaseDirectory = baseDir;
            logs.Add(LogFileType.Main, new LogFile("ModTek_runtime_log.txt", isDebug));
            //Log.logs.Add(LogFileType.Main, null);
            flushThread.Start();
        }
    }
}
