using System;
using System.Collections.Generic;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal class UnityLogHandlerAdapter : ILogHandler
    {
        private readonly ILog log;

        internal UnityLogHandlerAdapter(ILog log)
        {
            this.log = log;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            log.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            log.LogAtLevel(levelMapping[logType], string.Format(format, args), context);
        }

        private static readonly Dictionary<LogType, LogLevel> levelMapping = new() {
            {LogType.Log, LogLevel.Log},
            {LogType.Assert, LogLevel.Log},
            {LogType.Warning, LogLevel.Warning},
            {LogType.Error, LogLevel.Error},
            {LogType.Exception, LogLevel.Error}
        };
    }
}
