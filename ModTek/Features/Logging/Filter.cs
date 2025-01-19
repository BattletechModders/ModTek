using System;
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
            // Intern allows us to use ReferenceEquals a.k.a ==
            // since logger names are already interned by LogImpl
            _loggerNames = settings.LoggerNames.Select(string.Intern).ToArray();
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
        if (_loggerNames != null)
        {
            foreach (var loggerName in _loggerNames)
            {
                if (ReferenceEquals(loggerName, messageDto.LoggerName))
                {
                    return true;
                }
            }

            return false;
        }

        if (_logLevels != null)
        {
            foreach (var logLevel in _logLevels)
            {
                if (logLevel == messageDto.LogLevel)
                {
                    return true;
                }
            }

            return false;
        }

        if (_messagePrefixesMatcher != null && !_messagePrefixesMatcher.IsMatch(messageDto.Message))
        {
            return false;
        }

        return true;
    }
}