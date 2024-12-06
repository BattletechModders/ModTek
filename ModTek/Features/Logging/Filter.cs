using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal class Filter
{
    private readonly string[] _loggerNames;
    private readonly LogLevel[] _logLevels;
    private readonly Regex _messagePrefixesMatcher;

    internal Filter(FilterSettings settings)
    {
        if (settings.LoggerNames != null)
        {
            _loggerNames = settings.LoggerNames;
        }
        if (settings.LogLevels != null)
        {
            _logLevels = settings.LogLevels;
        }

        if (settings.MessagePrefixes != null)
        {
            try
            {
                var trie = Trie.Create(settings.MessagePrefixes);
                _messagePrefixesMatcher = trie.CompileRegex();
            }
            catch (Exception e)
            {
                throw new Exception("Issue processing logging ignore prefixes", e);
            }
        }
    }

    internal bool IsMatch(ref MTLoggerMessageDto messageDto)
    {
        if (_loggerNames != null && !_loggerNames.Contains(messageDto.LoggerName))
        {
            return false;
        }

        if (_logLevels != null && !_logLevels.Contains(messageDto.LogLevel))
        {
            return false;
        }

        if (_messagePrefixesMatcher != null && !_messagePrefixesMatcher.IsMatch(messageDto.Message))
        {
            return false;
        }

        return true;
    }
}