using System;
using Vintagestory.API.Common;

namespace AstriaPorta.Util;

[Flags]
public enum LogLevel
{
    None = 0,
    Info = 0x1,
    Debug = 0x2,
    Warning = 0x4,
    Error = 0x8
}

public static class GateLogger
{
    private static ILogger _internalLogger;
    private static LogLevel _loggingLevel = LogLevel.None;

    public static void Initialize(ILogger logger)
    {
        _internalLogger = logger;
    }

    public static void Log(LogLevel level, EnumLogType logType, string message)
    {
        if ((_loggingLevel & level) == 0) return;

        _internalLogger.Log(logType, message);
    }

    public static void LogAudit(LogLevel level, string message)
    {
        if ((_loggingLevel & level) == 0) return;

        _internalLogger.Audit(message);
    }

    public static void LogDebug(LogLevel level, string message)
    {
        if ((_loggingLevel & level) == 0) return;

        _internalLogger.Debug(message);
    }

    public static void LogError(LogLevel level, string message)
    {
        if ((_loggingLevel & level) == 0) return;

        _internalLogger.Error(message);
    }

    public static void LogWarning(LogLevel level, string message)
    {
        if ((_loggingLevel & level) == 0) return;

        _internalLogger.Warning(message);
    }

    public static void SetLogLevel(LogLevel level)
    {
        _loggingLevel = level;
    }
}
