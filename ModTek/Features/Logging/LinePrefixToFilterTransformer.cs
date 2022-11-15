using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal static class LinePrefixToFilterTransformer
{
    internal static List<Filter> CreateFilters(string[] prefixes)
    {
        var all = prefixes
            .Select(CreateFilterSettingsFromLinePrefix)
            .OrderBy(x => x.LoggerNames?[0])
            .ThenBy(x => x.LogLevels?[0])
            .ThenBy(x => x.MessagePrefixes?[0])
            .ToList();

        var filters = new List<Filter>();
        FilterSettings last = null;
        foreach (var current in all)
        {
            if (last == null)
            {
                last = current;
                continue;
            }

            if (MergeFullPrefix(last, current))
            {
                continue;
            }

            filters.Add(new Filter(last));
            last = current;
        }
        if (last != null)
        {
            filters.Add(new Filter(last));
        }
        return filters;
    }

    // legacy full line support
    private static FilterSettings CreateFilterSettingsFromLinePrefix(string linePrefix)
    {
        var regex = new Regex(@"^([^\]]+)(?: \[([^\]]+)\](?: (.+))?)?$");
        var match = regex.Match(linePrefix);
        if (!match.Success)
        {
            throw new ArgumentException($"Not a valid `PrefixesToIgnore` ({regex}) pattern: {linePrefix}");
        }

        var settings = new FilterSettings
        {
            LoggerNames = new[]
            {
                match.Groups[1].Value
            }
        };

        if (match.Groups[2].Success)
        {
            if (!Enum.TryParse(match.Groups[2].Value, true, out LogLevel logLevel))
            {
                throw new ArgumentException("Can't parse " + match.Groups[2].Value + ". Not a valid HBS log level");
            }

            settings.LogLevels = new[]
            {
                logLevel
            };

            if (match.Groups[3].Success)
            {
                settings.MessagePrefixes = new[]
                {
                    match.Groups[3].Value
                };
            }
        }

        return settings;
    }
    private static bool MergeFullPrefix(FilterSettings @this, FilterSettings other)
    {
        if (!@this.LoggerNames.SequenceEqual(other.LoggerNames))
        {
            return false;
        }

        if (@this.LogLevels == null)
        {
            return true;
        }

        if (!@this.LogLevels.SequenceEqual(other.LogLevels))
        {
            return false;
        }

        if (@this.MessagePrefixes == null)
        {
            return true;
        }

        @this.MessagePrefixes = @this.MessagePrefixes.Concat(other.MessagePrefixes).ToArray();
        return true;
    }
}