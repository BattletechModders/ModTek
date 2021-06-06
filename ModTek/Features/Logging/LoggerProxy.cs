using System;
using System.Collections.Generic;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal class LoggerProxy : ILogHandler
    {
        ILog hbslog;
        public LoggerProxy(ILog hbslog)
        {
            this.hbslog = hbslog;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            hbslog.LogException(exception, context);
        }

        static Dictionary<LogType, LogLevel> lmap = new() {
            {LogType.Log, LogLevel.Log},
            {LogType.Assert, LogLevel.Log},
            {LogType.Warning, LogLevel.Warning},
            {LogType.Error, LogLevel.Error},
            {LogType.Exception, LogLevel.Error}
        };
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            hbslog.LogAtLevel(lmap[logType], string.Format(format, args), context);
        }
    }
}
