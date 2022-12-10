using System.Collections.Generic;
using System.Linq;

namespace ModTek.Features.Logging;

internal abstract class AppenderBase
{
    private readonly Formatter _formatter;
    private readonly List<Filter> _includeFilters;
    private readonly List<Filter> _excludeFilters;

    internal AppenderBase(AppenderSettings settings)
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
        WriteLine(messageDto, logLine);
    }

    protected abstract void WriteLine(MTLoggerMessageDto messageDto, string line);
}