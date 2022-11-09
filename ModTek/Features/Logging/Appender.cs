using System;
using System.Globalization;
using System.IO;
using ModTek.Util;

namespace ModTek.Features.Logging
{
    internal class Appender : IDisposable
    {
        private readonly StreamWriter writer;

        internal Appender(string path)
        {
            FileUtils.CreateParentOfPath(path);
            FileUtils.RotatePath(path, 1);
            writer = new StreamWriter(path) { AutoFlush = true };
            writer.WriteLine($"ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
            writer.WriteLine(DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
            writer.WriteLine(new string('-', 80));
            writer.WriteLine(VersionInfo.GetFormattedInfo());
        }

        public void Dispose()
        {
            lock(this)
            {
                writer?.Dispose();
            }
        }

        internal void WriteLine(string line)
        {
            lock(this)
            {
                writer.WriteLine(line);
            }
        }
    }
}
