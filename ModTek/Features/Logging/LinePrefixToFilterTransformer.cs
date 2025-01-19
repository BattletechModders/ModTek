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
        return prefixes
            .OrderBy(x => x, StringComparer.Ordinal)
            .Distinct()
            .Select(CreateFilterSettingsFromLinePrefix)
            .ToList();
    }

    private static FilterSettings CreateFilterSettingsFromLinePrefix(string linePrefix)
    {
        var regex = new Regex(@"^([^\]]+)(?: \[([^\]]+)\](?: (.+))?)?$");
        var match = regex.Match(linePrefix);
        if (!match.Success)
        {
            throw new ArgumentException($"Not can't match pattern ({regex}) against value: {linePrefix}");
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