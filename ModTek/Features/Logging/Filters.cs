using System.Linq;

namespace ModTek.Features.Logging;

internal class Filters
{
    private readonly Filter[] _includeFilters;
    private readonly Filter[] _excludeFilters;

    internal Filters(AppenderSettings settings)
    {
        if (settings.Includes != null)
        {
            _includeFilters = settings.Includes.Select(fs => new Filter(fs)).ToArray();
        }
        if (settings.Excludes != null)
        {
            _excludeFilters = settings.Excludes.Select(fs => new Filter(fs)).ToArray();
        }

        // support old prefixes style
        if (settings.PrefixesToIgnore != null)
        {
            var legacyFilters = LinePrefixToFilterTransformer.CreateFilters(settings.PrefixesToIgnore).ToArray();
            _excludeFilters = _excludeFilters == null ? legacyFilters : [.._excludeFilters, ..legacyFilters];
        }
    }

    internal bool IsIncluded(ref MTLoggerMessageDto messageDto)
    {
        if (_includeFilters != null)
        {
            var found = false;
            foreach (var filter in _includeFilters)
            {
                if (filter.IsMatch(ref messageDto))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        if (_excludeFilters != null)
        {
            foreach (var filter in _excludeFilters)
            {
                if (filter.IsMatch(ref messageDto))
                {
                    return false;
                }
            }
        }

        return true;
    }
}