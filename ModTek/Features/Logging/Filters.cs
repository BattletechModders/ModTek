using System.Collections.Generic;
using System.Linq;

namespace ModTek.Features.Logging;

internal class Filters
{
    private readonly List<Filter> _includeFilters;
    private readonly List<Filter> _excludeFilters;

    internal Filters(AppenderSettings settings)
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
    }

    internal bool IsIncluded(MTLoggerMessageDto messageDto)
    {
        if (_includeFilters != null && !_includeFilters.Any(x => x.IsMatch(messageDto)))
        {
            return false;
        }

        if (_excludeFilters != null && _excludeFilters.Any(x => x.IsMatch(messageDto)))
        {
            return false;
        }

        return true;
    }
}