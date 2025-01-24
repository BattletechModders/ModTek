using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
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

        if (settings.Include is { Length: > 0 } || settings.Exclude is { Length: > 0 })
        {
            var filterBuilder = new FilterBuilder(il);
            if (settings.Include is { Length: > 0 })
            {
                var filterSettings = LinePrefixToFilterTransformer.CreateFilters(settings.Include).ToArray();
                filterBuilder.AddFilter(filterSettings, true);
            }
            if (settings.Exclude is { Length: > 0 })
            {
                var filterSettings = LinePrefixToFilterTransformer.CreateFilters(settings.Exclude).ToArray();
                filterBuilder.AddFilter(filterSettings, false);
            }
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4_1);
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

    private void AddFilter(FilterSettings[] filterSettings, bool positive)
    {
        OpCode isMatchRetValue;
        OpCode noMatchRetValue;
        if (positive)
        {
            isMatchRetValue = OpCodes.Ldc_I4_1;
            noMatchRetValue = OpCodes.Ldc_I4_0;
        }
        else
        {
            isMatchRetValue = OpCodes.Ldc_I4_0;
            noMatchRetValue = OpCodes.Ldc_I4_1;
        }

        Label? labelToNextLoggerName = null;
        Label? labelToNextLogLevel = null;
        Label? labelToNextMessagePrefix = null;
        var previousFilter = filterSettings[0];
        {
            var filter = filterSettings[0];
            _il.Emit(OpCodes.Ldloc, _loggerNameLocalBuilder);
            _il.Emit(OpCodes.Ldstr, filter.LoggerName);
            labelToNextLoggerName = _il.DefineLabel();
            _il.Emit(OpCodes.Bne_Un_S, labelToNextLoggerName.Value);

            if (filter.LogLevel != null)
            {
                _il.Emit(OpCodes.Ldloc, _logLevelLocalBuilder);
                _il.Emit(OpCodes.Ldc_I4, (int)filter.LogLevel.Value);
                labelToNextLogLevel = _il.DefineLabel();
                _il.Emit(OpCodes.Bne_Un_S, labelToNextLogLevel.Value);
            }
            else
            {
                _il.Emit(isMatchRetValue);
                _il.Emit(OpCodes.Ret);
            }

            if (filter.MessagePrefix != null)
            {
                _il.Emit(OpCodes.Ldloc, _messageLocalBuilder);
                _il.Emit(OpCodes.Ldstr, filter.MessagePrefix);
                _il.Emit(OpCodes.Call, s_fastStartWithMethodInfo);
                labelToNextMessagePrefix = _il.DefineLabel();
                _il.Emit(OpCodes.Brfalse_S, labelToNextMessagePrefix.Value);
            }
            else
            {
                _il.Emit(isMatchRetValue);
                _il.Emit(OpCodes.Ret);
            }
        }
        for (var i = 1; i < filterSettings.Length; i++)
        {
            var filter = filterSettings[i];
            if (previousFilter.LoggerName != filter.LoggerName)
            {
                if (labelToNextLogLevel != null || labelToNextMessagePrefix != null)
                {
                    if (labelToNextLogLevel != null)
                    {
                        _il.MarkLabel(labelToNextLogLevel.Value);
                        labelToNextLogLevel = null;
                    }
                    if (labelToNextMessagePrefix != null)
                    {
                        _il.MarkLabel(labelToNextMessagePrefix.Value);
                        labelToNextMessagePrefix = null;
                    }
                    _il.Emit(noMatchRetValue);
                    _il.Emit(OpCodes.Ret);
                }
                _il.MarkLabel(labelToNextLoggerName.Value);
                _il.Emit(OpCodes.Ldloc, _loggerNameLocalBuilder);
                _il.Emit(OpCodes.Ldstr, filter.LoggerName);
                labelToNextLoggerName = _il.DefineLabel();
                _il.Emit(OpCodes.Bne_Un_S, labelToNextLoggerName.Value);
            }

            if (labelToNextLogLevel == null || previousFilter.LogLevel != filter.LogLevel)
            {
                if (labelToNextMessagePrefix != null)
                {
                    _il.MarkLabel(labelToNextMessagePrefix.Value);
                    labelToNextMessagePrefix = null;
                    _il.Emit(noMatchRetValue);
                    _il.Emit(OpCodes.Ret);
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
                _il.Emit(OpCodes.Bne_Un_S, labelToNextLogLevel.Value);
            }
            else
            {
                _il.Emit(isMatchRetValue);
                _il.Emit(OpCodes.Ret);
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
                _il.Emit(OpCodes.Brfalse_S, labelToNextMessagePrefix.Value);
            }
            _il.Emit(isMatchRetValue);
            _il.Emit(OpCodes.Ret);
        }

        if (labelToNextLogLevel != null)
        {
            _il.MarkLabel(labelToNextLogLevel.Value);
        }
        if (labelToNextMessagePrefix != null)
        {
            _il.MarkLabel(labelToNextMessagePrefix.Value);
        }
        _il.MarkLabel(labelToNextLoggerName.Value);
        _il.Emit(noMatchRetValue);
        _il.Emit(OpCodes.Ret);
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
            return false;
        }

        // logger + 2 loglevel + 2x2 prefix
        if (ReferenceEquals(loggerName, "Analytics"))
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
                return false;
            }
            if (logLevel == LogLevel.Error)
            {
                if (message.FastStartsWith("Analytics Event requested with invalid IP"))
                {
                    return true;
                }
                if (message.FastStartsWith("Request next called but reporting is disabled"))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        // logger + 0 loglevel + 0 prefix
        if (ReferenceEquals(loggerName, "Last"))
        {
            return true;
        }

        return false;
    }
}