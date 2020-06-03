using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ModTekInstaller
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
                this.mutex = new Mutex();
                this.enabled = enabled;
                this.m_cache = new StringBuilder();
                this.m_logfile = Path.Combine(Log.BaseDirectory, name);
                File.Delete(this.m_logfile);
                this.m_fs = new StreamWriter(this.m_logfile);
                this.m_fs.AutoFlush = true;
            }
            catch (Exception)
            {

            }
        }
        public void flush()
        {
            if (this.mutex.WaitOne(1000))
            {
                this.m_fs.Write(this.m_cache.ToString());
                this.m_fs.Flush();
                this.m_cache.Length = 0;
                this.mutex.ReleaseMutex();
            }
        }
        public void W(string line, bool isCritical = true)
        {
            this.m_fs.Write(line);
            this.m_fs.Flush();
        }
        public void WL(string line, bool isCritical = true)
        {
            line += "\n"; this.W(line, isCritical);
        }
        public void W(int initiation, string line, bool isCritical = false)
        {
            string init = new string(' ', initiation);
            line = init + line; this.W(line, isCritical);
        }
        public void WL(int initiation, string line, bool isCritical = true)
        {
            string init = new string(' ', initiation);
            line = init + line; this.WL(line, isCritical);
        }
        public void TW(int initiation, string line, bool isCritical = true)
        {
            string init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            this.W(line, isCritical);
        }
        public void TWL(int initiation, string line, bool isCritical = true)
        {
            string init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            this.WL(line, isCritical);
        }
    }
    public static class Log
    {
        private static Dictionary<LogFileType, LogFile> logs = new Dictionary<LogFileType, LogFile>();
        //private static string m_assemblyFile;
        public static string BaseDirectory;
        public static readonly int flushBufferLength = 16 * 1024;
        public static bool flushThreadActive = true;
        //public static Thread flushThread = new Thread(flushThreadProc);
        public static void flushThreadProc()
        {
            while (Log.flushThreadActive == true)
            {
                //Thread.Sleep(30 * 1000);
                //Log.LogWrite("Log flushing\n");
                //Log.flush();
            }
        }
        //public static void flush()
        //{
        //    foreach (var log in Log.logs) { log.Value.flush(); }
        //}
        public static void LogWrite(string line, bool isCritical = false)
        {
            if (Log.logs.ContainsKey(LogFileType.Main) == false) { return; }
            Log.logs[LogFileType.Main].W(line, isCritical);
        }
        public static LogFile M { get { return Log.logs[LogFileType.Main]; } }
        public static void InitLog()
        {
            //LogFile file = new LogFile("CAC_main_log.txt", CustomAmmoCategories.Settings.debugLog);
            Log.logs.Add(LogFileType.Main, new LogFile("ModTekInstaller_main_log.txt", true));
            //Log.logs.Add(LogFileType.Main, null);
            //Log.flushThread.Start();
        }
    }

}
