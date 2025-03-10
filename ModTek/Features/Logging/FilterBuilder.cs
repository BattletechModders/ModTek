﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HBS.Logging;

namespace ModTek.Features.Logging;

// array + dict based filters: up to 300 ns
// LambdaExpressions are bugged when used with Call -> probably brutal memory leak, starts at 10us and only goes up
// hardcoded: 12ns
// dynamic method via ILGenerator + with lots of iterations for JIT: 12ns
internal class FilterBuilder
{
    internal delegate bool FilterDelegate(ref MTLoggerMessageDto messageDto);

    internal static FilterDelegate Compile(AppenderSettings settings)
    {
        var dynamicMethod = new DynamicMethod(
            "IsMatch", // methods with the same name are allowed
            typeof(bool),
            [typeof(MTLoggerMessageDto).MakeByRefType()],
            typeof(FilterBuilder),
            true
        );

        var il = dynamicMethod.GetILGenerator();
        var filterBuilder = new FilterBuilder(il);

        if (settings.Exclude is { Length: > 0 })
        {
            var filterSettings = LinePrefixToFilterTransformer.CreateFilters(settings.Exclude).ToArray();
            filterBuilder.AddFilter(filterSettings, OpCodes.Ldc_I4_0); // return false if matched
        }

        if (settings.Include is { Length: > 0 })
        {
            var filterSettings = LinePrefixToFilterTransformer.CreateFilters(settings.Include).ToArray();
            filterBuilder.AddFilter(filterSettings, OpCodes.Ldc_I4_1); // return true if matched

            il.Emit(OpCodes.Ldc_I4_0); // return false if not matched
            il.Emit(OpCodes.Ret);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4_1); // return true if not matched
            il.Emit(OpCodes.Ret);
        }

        return dynamicMethod.CreateDelegate(typeof(FilterDelegate)) as FilterDelegate;
    }

    private readonly ILGenerator _il;
    private readonly LocalBuilder _loggerNameLocalBuilder;
    private readonly LocalBuilder _logLevelLocalBuilder;
    private readonly LocalBuilder _messageLocalBuilder;

    private FilterBuilder(ILGenerator il)
    {
        this._il = il;

        _loggerNameLocalBuilder = il.DeclareLocal(typeof(string));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(MTLoggerMessageDto), nameof(MTLoggerMessageDto.LoggerName)));
        il.Emit(OpCodes.Stloc, _loggerNameLocalBuilder);

        _logLevelLocalBuilder = il.DeclareLocal(typeof(LogLevel));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(MTLoggerMessageDto), nameof(MTLoggerMessageDto.LogLevel)));
        il.Emit(OpCodes.Stloc, _logLevelLocalBuilder);

        _messageLocalBuilder = il.DeclareLocal(typeof(string));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, AccessTools.Field(typeof(MTLoggerMessageDto), nameof(MTLoggerMessageDto.Message)));
        il.Emit(OpCodes.Stloc, _messageLocalBuilder);
    }

    private void AddFilter(FilterSettings[] filterSettings, OpCode isMatchRetValue)
    {
        HashSet<Label> noMatchLabels = [];

        Label? labelToNextLoggerName;
        Label? labelToNextLogLevel = null;
        Label? labelToNextMessagePrefix = null;
        var previousFilter = filterSettings[0];
        {
            var filter = filterSettings[0];
            _il.Emit(OpCodes.Ldloc, _loggerNameLocalBuilder);
            _il.Emit(OpCodes.Ldstr, filter.LoggerName);
            labelToNextLoggerName = _il.DefineLabel();
            _il.Emit(OpCodes.Bne_Un, labelToNextLoggerName.Value);

            if (filter.LogLevel != null)
            {
                _il.Emit(OpCodes.Ldloc, _logLevelLocalBuilder);
                _il.Emit(OpCodes.Ldc_I4, (int)filter.LogLevel.Value);
                labelToNextLogLevel = _il.DefineLabel();
                _il.Emit(OpCodes.Bne_Un, labelToNextLogLevel.Value);
            }

            if (filter.MessagePrefix != null)
            {
                _il.Emit(OpCodes.Ldloc, _messageLocalBuilder);
                _il.Emit(OpCodes.Ldstr, filter.MessagePrefix);
                _il.Emit(OpCodes.Call, s_fastStartWithMethodInfo);
                labelToNextMessagePrefix = _il.DefineLabel();
                _il.Emit(OpCodes.Brfalse, labelToNextMessagePrefix.Value);
            }

            _il.Emit(isMatchRetValue);
            _il.Emit(OpCodes.Ret);
        }
        for (var i = 1; i < filterSettings.Length; i++)
        {
            var filter = filterSettings[i];
            if (previousFilter.LoggerName != filter.LoggerName)
            {
                if (labelToNextLogLevel != null)
                {
                    noMatchLabels.Add(labelToNextLogLevel.Value);
                    labelToNextLogLevel = null;
                }
                if (labelToNextMessagePrefix != null)
                {
                    noMatchLabels.Add(labelToNextMessagePrefix.Value);
                    labelToNextMessagePrefix = null;
                }
                _il.MarkLabel(labelToNextLoggerName.Value);
                _il.Emit(OpCodes.Ldloc, _loggerNameLocalBuilder);
                _il.Emit(OpCodes.Ldstr, filter.LoggerName);
                labelToNextLoggerName = _il.DefineLabel();
                _il.Emit(OpCodes.Bne_Un, labelToNextLoggerName.Value);
            }
            else if (previousFilter.LogLevel != filter.LogLevel)
            {
                if (labelToNextMessagePrefix != null)
                {
                    noMatchLabels.Add(labelToNextMessagePrefix.Value);
                    labelToNextMessagePrefix = null;
                }
            }

            if (filter.LogLevel != null)
            {
                if (labelToNextLogLevel != null)
                {
                    _il.MarkLabel(labelToNextLogLevel.Value);
                }
                _il.Emit(OpCodes.Ldloc, _logLevelLocalBuilder);
                _il.Emit(OpCodes.Ldc_I4, (int)filter.LogLevel.Value);
                labelToNextLogLevel = _il.DefineLabel();
                _il.Emit(OpCodes.Bne_Un, labelToNextLogLevel.Value);
            }

            if (filter.MessagePrefix != null)
            {
                if (labelToNextMessagePrefix != null)
                {
                    _il.MarkLabel(labelToNextMessagePrefix.Value);
                }
                _il.Emit(OpCodes.Ldloc, _messageLocalBuilder);
                _il.Emit(OpCodes.Ldstr, filter.MessagePrefix);
                _il.Emit(OpCodes.Call, s_fastStartWithMethodInfo);
                labelToNextMessagePrefix = _il.DefineLabel();
                _il.Emit(OpCodes.Brfalse, labelToNextMessagePrefix.Value);
            }

            _il.Emit(isMatchRetValue);
            _il.Emit(OpCodes.Ret);

            previousFilter = filter;
        }

        noMatchLabels.Add(labelToNextLoggerName.Value);
        if (labelToNextLogLevel != null)
        {
            noMatchLabels.Add(labelToNextLogLevel.Value);
        }
        if (labelToNextMessagePrefix != null)
        {
            noMatchLabels.Add(labelToNextMessagePrefix.Value);
        }

        foreach (var noMatchLabel in noMatchLabels)
        {
            _il.MarkLabel(noMatchLabel);
        }
    }

    private static readonly MethodInfo s_fastStartWithMethodInfo =
        SymbolExtensions.GetMethodInfo(() => "".FastStartsWith(""));

    // ReSharper disable once UnusedMember.Local
    // template used to find code IL above
    private static bool HardcodedFilter(ref MTLoggerMessageDto messageDto)
    {
        var loggerName = messageDto.LoggerName;
        var logLevel = messageDto.LogLevel;
        var message = messageDto.Message;

        // logger + 1 loglevel + 0 prefix
        if (ReferenceEquals(loggerName, "Achievements"))
        {
            if (logLevel == LogLevel.Log)
            {
                return true;
            }
        }

        // logger + 2 loglevel + 2x2 prefix
        else if (ReferenceEquals(loggerName, "Analytics"))
        {
            if (logLevel == LogLevel.Warning)
            {
                if (message.FastStartsWith("Request next called but no servers have been found"))
                {
                    return true;
                }
                if (message.FastStartsWith("Request next called but reporting is disabled"))
                {
                    return true;
                }
            }
            else if (logLevel == LogLevel.Error)
            {
                if (message.FastStartsWith("Analytics Event requested with invalid IP"))
                {
                    return true;
                }
                if (message.FastStartsWith("Request next called but reporting is disabled"))
                {
                    return true;
                }
            }
        }

        // logger + 0 loglevel + 0 prefix
        else if (ReferenceEquals(loggerName, "Last"))
        {
            return true;
        }

        return false;
    }
}