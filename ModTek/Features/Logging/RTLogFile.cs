using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ModTek.Features.Logging
{
    internal class RTLogFile
    {
        private string m_logfile;
        private Mutex mutex;
        private StringBuilder m_cache;
        private StreamWriter m_fs;
        private bool enabled;

        public RTLogFile(string name, bool enabled)
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
}
