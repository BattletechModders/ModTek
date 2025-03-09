using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HBS.Logging;
using ModTek.Public;

namespace ModTek.Features.Logging;

internal static class LinePrefixToFilterTransformer
{
    internal static List<FilterSettings> CreateFilters(string[] prefixes)
    {
        FilterSettings previous = null;
        return prefixes
            .OrderBy(s => s)
            .Distinct()
            .Select(CreateFilterSettingsFromLinePrefix)
            .OrderBy(f => f.LoggerName, StringComparer.Ordinal)
            .ThenBy(f => f.LogLevel)
            .ThenBy(f => f.MessagePrefix, StringComparer.Ordinal)
            .Where(f =>
            {
                var ok = true;
                if (previous != null)
                {
                    ok = !AlreadyIncludes(previous, f);
                }
                previous = f;
                return ok;
            })
            .ToList();
    }

    private static bool AlreadyIncludes(FilterSettings b, FilterSettings i)
    {
        if (b.LoggerName == i.LoggerName)
        {
            if (b.LogLevel == null)
            {
                return true;
            }

            if (b.LogLevel == i.LogLevel)
            {
                if (b.MessagePrefix == null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static FilterSettings CreateFilterSettingsFromLinePrefix(string linePrefix)
    {
        var regex = new Regex(@"^(.+?)(?: \[([^\]]+)\](?: (.+))?)?$");
        var match = regex.Match(linePrefix);
        if (!match.Success)
        {
            throw new ArgumentException($"Can't match pattern ({regex}) against value: {linePrefix}");
        }

        var settings = new FilterSettings
        {
            LoggerName = string.Intern(match.Groups[1].Value)
        };

        if (match.Groups[2].Success)
        {
            if (!LogLevelExtension.TryParse(match.Groups[2].Value, out var logLevel))
            {
                throw new ArgumentException("Can't parse " + match.Groups[2].Value + ". Not a valid HBS log level");
            }

            settings.LogLevel = logLevel;

            if (match.Groups[3].Success)
            {
                settings.MessagePrefix = match.Groups[3].Value;
            }
        }

        return settings;
    }
}