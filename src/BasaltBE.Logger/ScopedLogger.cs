using BasaltBE.Logging.Enums;
using BasaltBE.Logging.Types;
using System.Runtime.CompilerServices;

namespace BasaltBE.Logging;

public readonly struct ScopedLogger(string scope) {
    public readonly string Scope = scope;

    public void Debug(string message) => Logger.Write(LogLevel.Debug, Scope, message);
    public void Info(string message) => Logger.Write(LogLevel.Info, Scope, message);
    public void Warn(string message) => Logger.Write(LogLevel.Warn, Scope, message);
    public void Error(string message) => Logger.Write(LogLevel.Error, Scope, message);

    public bool IsEnabled(LogLevel level) => Logger.IsEnabled(level);

    public void Debug([InterpolatedStringHandlerArgument("")] ref DebugLogMessageHandler message) {
        if (message.Enabled) Logger.Write(LogLevel.Debug, Scope, message.GetMessage());
    }

    public void Info([InterpolatedStringHandlerArgument("")] ref InfoLogMessageHandler message) {
        if (message.Enabled) Logger.Write(LogLevel.Info, Scope, message.GetMessage());
    }

    public void Warn([InterpolatedStringHandlerArgument("")] ref WarnLogMessageHandler message) {
        if (message.Enabled) Logger.Write(LogLevel.Warn, Scope, message.GetMessage());
    }

    public void Error([InterpolatedStringHandlerArgument("")] ref ErrorLogMessageHandler message) {
        if (message.Enabled) Logger.Write(LogLevel.Error, Scope, message.GetMessage());
    }
}

