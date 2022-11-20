using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ModTek.Public;
using ModTek.Util;

namespace ModTek.Features.Logging;

internal class LogAppender : IDisposable
{
    private readonly Formatter _formatter;
    private readonly StreamWriter _writer;
    private readonly List<Filter> _includeFilters;
    private readonly List<Filter> _excludeFilters;

    internal LogAppender(string filePath, AppenderSettings settings)
    {
        var path =  Path.Combine(FilePaths.TempModTekDirectory, filePath);

        FileUtils.CreateParentOfPath(path);
        FileUtils.RotatePath(path, settings.LogRotationCount);
        _writer = new StreamWriter(path) { AutoFlush = true };
        _writer.WriteLine($"ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        _writer.WriteLine(DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
        _writer.WriteLine(new string('-', 80));
        _writer.WriteLine(VersionInfo.GetFormattedInfo());

        try
        {
            if (settings.Includes != null)
            {
                _includeFilters = settings.Includes.Select(fs => new Filter(fs)).ToList();
            }
            if (settings.Excludes != null)
            {
                _excludeFilters = settings.Excludes.Select(fs => new Filter(fs)).ToList();
            }

            // support old prefixes style
            if (settings.PrefixesToIgnore != null)
            {
                var legacyFilters = LinePrefixToFilterTransformer.CreateFilters(settings.PrefixesToIgnore);
                if (_excludeFilters == null)
                {
                    _excludeFilters = legacyFilters;
                }
                else
                {
                    _excludeFilters.AddRange(legacyFilters);
                }
            }

            _formatter = new Formatter(settings);
        }
        catch (Exception e)
        {
            WriteLine(e.ToString());
        }
    }

    public void Dispose()
    {
        lock(this)
        {
            _writer?.Dispose();
        }
    }

    private void WriteLine(string line)
    {
        lock(this)
        {
            _writer.WriteLine(line);
        }
    }

    internal void Append(MTLoggerMessageDto messageDto)
    {
        if (_includeFilters != null && !_includeFilters.Any(x => x.IsMatch(messageDto)))
        {
            return;
        }

        if (_excludeFilters != null && _excludeFilters.Any(x => x.IsMatch(messageDto)))
        {
            return;
        }

        var logLine = _formatter?.GetFormattedLogLine(messageDto) ?? messageDto.message;
        WriteLine(logLine);
    }
}