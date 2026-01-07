using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace AstriaPorta.Util;

public enum LogLevel
{
    None,
    Low,
    Medium,
    High
}

public static class GateLogger
{
    private static ILogger _internalLogger;
    private static LogLevel _loggingLevel = LogLevel.None;

    public static void Initialize(ILogger logger)
    {
        _internalLogger = logger;
    }

    public static void Log(LogLevel minimumLevel, EnumLogType logType, string message)
    {
        if (_loggingLevel < minimumLevel) return;

        _internalLogger.Log(logType, message);
    }

    public static void LogAudit(LogLevel minimumLevel, string message)
    {
        if (_loggingLevel < minimumLevel) return;

        _internalLogger.Audit(message);
    }

    public static void LogDebug(LogLevel minimumLevel, string message)
    {
        if (_loggingLevel < minimumLevel) return;

        _internalLogger.Debug(message);
    }

    public static void LogError(LogLevel minimumLevel, string message)
    {
        if (_loggingLevel < minimumLevel) return;

        _internalLogger.Error(message);
    }

    public static void LogWarning(LogLevel minimumLevel, string message)
    {
        if (_loggingLevel < minimumLevel) return;

        _internalLogger.Warning(message);
    }

    public static void SetLogLevel(LogLevel level)
    {
        _loggingLevel = level;
    }
}
