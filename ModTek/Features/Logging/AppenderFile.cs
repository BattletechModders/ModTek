using System;
using System.Globalization;
using System.IO;
using ModTek.Public;
using ModTek.Util;

namespace ModTek.Features.Logging;

internal class AppenderFile : AppenderBase, IDisposable
{
    private readonly StreamWriter _writer;

    internal AppenderFile(string filePath, AppenderSettings settings) : base(settings)
    {
        var path =  Path.Combine(FilePaths.TempModTekDirectory, filePath);

        FileUtils.CreateParentOfPath(path);
        FileUtils.RotatePath(path, settings.LogRotationCount);
        _writer = new StreamWriter(path) { AutoFlush = true };
        _writer.WriteLine($"ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        _writer.WriteLine(DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
        _writer.WriteLine(new string('-', 80));
        _writer.WriteLine(VersionInfo.GetFormattedInfo());
    }

    public void Dispose()
    {
        lock(this)
        {
            _writer?.Dispose();
        }
    }

    protected override void WriteLine(MTLoggerMessageDto messageDto, string line)
    {
        lock(this)
        {
            _writer.WriteLine(line);
        }
    }
}