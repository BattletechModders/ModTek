using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal class Filter
{
    private readonly HashSet<string> _loggerNames;
    private readonly HashSet<LogLevel> _logLevels;
    private readonly Regex _messagePrefixesMatcher;

    internal Filter(FilterSettings settings)
    {
        if (settings.LoggerNames != null)
        {
            _loggerNames = new HashSet<string>(settings.LoggerNames);
        }
        if (settings.LogLevels != null)
        {
            _logLevels = new HashSet<LogLevel>(settings.LogLevels);
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

    internal bool IsMatch(MTLoggerMessageDto messageDto)
    {
        if (_loggerNames != null && !_loggerNames.Contains(messageDto.loggerName))
        {
            return false;
        }

        if (_logLevels != null && !_logLevels.Contains(messageDto.logLevel))
        {
            return false;
        }

        if (_messagePrefixesMatcher != null && !_messagePrefixesMatcher.IsMatch(messageDto.message))
        {
            return false;
        }

        return true;
    }
}