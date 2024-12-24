# ModTek Logging

ModTek logging has the following features:
- Centralizes logging of Unity, BattleTech and any mods using the HBS Logging Infrastructure into one file. See `.modtek/battletech_log.txt`.
- Improves the performance by offloading the formatting and writing of log statements onto a thread away from the main Unity thread.
- Provides filter options to determine what does or does not get into the log.
- Formats a log message so that it includes a logger name, nicely formatted time, thread id if not on main thread, log message and possibly an exception.
- Uses a high precision timer, to allow 1/10 of microsecond precision (10^-7) for time stamps instead of milliseconds (10^-3).
- Supports adding additional log files through `ModTek/config.json`.
- Added more log levels, use "200" for Trace logging and "250" for Fatal logging. In C# enums are ints, so casting a integer to HBS' LogLevel is valid, `(LogLevel)200`.
- Log rotation per game start. Logs are rotated to survive at least one other start, logs that end with `.1` are from a previous application start.

## How to use

In a mod, just use the HBS logger.

```csharp
var logger = Logger.GetLogger("YourMod");
logger.Log("I have logged something");
```

Then set the log level using the debug settings, an advanced json merge example on how to add a mods logger to the configuration:

```json
{
    "TargetID": "settings",
    "Instructions": [
        {
            "JSONPath": "loggerLevels",
            "Action": "ArrayAdd",
            "Value":
            {
                "k" : "YourMod",
                "v" : "Debug"
            }
        }
    ]
}
```

Afterwards the log statements should appear in `.modtek/battletech_log.txt`.

### Trace Logging

Trace logging is just a log level. Meaning if you can work with the existing log levels, there is no need to require trace logging.
Some mods exhaust the existing log levels easily, and require a differentiation between log statements that helps with debugging (DEBUG), and log everything (TRACE/200).

Trace logging is used as follows:
```csharp
logger.LogAtLevel((LogLevel)200, "This is a trace log statement");
```

In the advanced merge json example from before, set 200 as the log level for your mod.
```json
{
    "TargetID": "settings",
    "Instructions": [
        {
            "JSONPath": "loggerLevels",
            "Action": "ArrayAdd",
            "Value":
            {
                "k" : "YourMod",
                "v" : "200"
            }
        }
    ]
}
```

### Mod Local Logs

> **Note**
> It is not recommended to use this feature as it produces duplicate IO

If a mod author wants to have a separate copy of all logs a logger produces, one can enable a separate log in `mod.json`:
```json
{
  "Log": {
    "FilePath": "log.txt",
    "LoggerName": "YourMod"
  }
}
```

### Nullable loggers

> **Note**
> Nullable loggers are a convenience feature and mainly for advanced users who are faced with log spam and unreadable code.

The best way of improving logging performance is not to log at all, or at least when requested.
The simplest way is to use the `Is*Enabled` flags to avoid expensive operations when logging is disabled for a log level.

```csharp
// init
var logger = Logger.GetLogger("YourMod");

// somewhere in code
if (logger.IsDebugEnabled)
{
   logger.LogDebug(ExpensiveOperationToCreateAString());
}
```

In C# one can use Null-conditional operators to check for null and skip code from executing.
When applied to logging, it makes for more readable code.

With nullable loggers:
```csharp
// init
var logger = Logger.GetLogger("YourMod");
var nullableLogger = logger.IsDebugEnabled ? logger : null;

// somewhere in code
nullableLogger?.Log(ExpensiveOperationToCreateAString());
```

Behind the scenes, C# actually translates that to:
```csharp
// somewhere in code (alternative view)
if (nullableLogger != null)
{
    nullableLogger.Log(ExpensiveOperationToCreateAString());
}
```

ModTek provides a utility class called [NullableLogger](../ModTek/Public/NullableLogger.cs) that makes it easy to setup
nullable logging in your mod.
An example of how to use `NullableLogger` can be found in [Log.cs](../ModTek/Log.cs).
The `NullableLogger` keeps track of changes to log levels and adjusts itself according,
as repeated `Is*Enabled` checks are slower than simple null checks.
