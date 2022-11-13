# ModTek Logging

ModTek logging has the following features:
- Centralizes logging of Unity, BattleTech and any mods using the HBS Logging Infrastructure into one file. See `.modtek/battletech_log.txt`.
- Improves the performance by offloading the formatting and writing of log statements onto a thread away from the main Unity thread.
- Provides filter options to determine what does or does not get into the log.
- Formats a log message so that it includes a logger name, nicely formatted time, thread id if not on main thread, log message and possibly an exception.
- Uses a high precision timer, to allow sub-microsecond precision for the time stamps. Standard is just milliseconds. Thats 10000 times more precision!
- Supports adding additional log files. (TODO allow mods to define the log files directly in mod.json, right now only possible via `ModTek/config.json`)
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
Some mods exhaust the existing log levels easily, and require a differentiation between log stuff that helps with debugging (DEBUG), and log everything (TRACE/200).

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

### Nullable loggers

Another way of enhancing performance is using nullable loggers. This is only for advanced users requiring lots of performance while still wanting readable code.

In C# one can use Null-conditional operators to check for null and skip code from executing.
This is very useful in logging, as to avoid logging in debug and trace levels, one usually checked the log levels of a logger before actually doing expensive calculations.

Old style:
```csharp
// init
var logger = Logger.GetLogger("YourMod");

// somewhere in code
if (logger.IsDebugEnabled)
{
   logger.LogDebug(ExpensiveOperationToCreateAString());
}
```

With nullable loggers:
```csharp
// init
var logger = Logger.GetLogger("YourMod");
var nullableLogger = logger.IsDebugEnabled ? logger : null;

// somewhere in code
nullableLogger?.ExpensiveOperationToCreateAString();
```

Behind the scenes, C# actually translates that to:
```csharp
// somewhere in code
if (nullableLogger != null)
{
    nullableLogger.ExpensiveOperationToCreateAString();
}
```

There is a caveat, the game changes log levels after the mods have initialized, so in order to keep up there is a Harmony patch required and some boilerplate code.

See `Logging.cs` in e.g. MechEngineer or CustomFilters to snag class that works out of the box.
