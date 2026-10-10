using System.Runtime.CompilerServices;
using BasaltBE.Logging.Enums;

namespace BasaltBE.Logging.Types;

[InterpolatedStringHandler]
public ref struct DebugLogMessageHandler {
    private DefaultInterpolatedStringHandler message;
    internal bool Enabled;

    public DebugLogMessageHandler(int literalLength, int formattedCount, ScopedLogger logger, out bool shouldAppend) {
        Enabled = Logger.IsEnabled(LogLevel.Debug);
        shouldAppend = Enabled;
        message = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
    }

    public void AppendLiteral(string value) => message.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => message.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string? format) => message.AppendFormatted(value, format);
    public void AppendFormatted<T>(T value, int alignment) => message.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format) => message.AppendFormatted(value, alignment, format);
    public void AppendFormatted(ReadOnlySpan<char> value) => message.AppendFormatted(value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) => message.AppendFormatted(value, alignment);
    internal string GetMessage() => message.ToStringAndClear();
}

[InterpolatedStringHandler]
public ref struct InfoLogMessageHandler {
    private DefaultInterpolatedStringHandler message;
    internal bool Enabled;

    public InfoLogMessageHandler(int literalLength, int formattedCount, ScopedLogger logger, out bool shouldAppend) {
        Enabled = Logger.IsEnabled(LogLevel.Info);
        shouldAppend = Enabled;
        message = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
    }

    public void AppendLiteral(string value) => message.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => message.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string? format) => message.AppendFormatted(value, format);
    public void AppendFormatted<T>(T value, int alignment) => message.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format) => message.AppendFormatted(value, alignment, format);
    public void AppendFormatted(ReadOnlySpan<char> value) => message.AppendFormatted(value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) => message.AppendFormatted(value, alignment);
    internal string GetMessage() => message.ToStringAndClear();
}

[InterpolatedStringHandler]
public ref struct WarnLogMessageHandler {
    private DefaultInterpolatedStringHandler message;
    internal bool Enabled;

    public WarnLogMessageHandler(int literalLength, int formattedCount, ScopedLogger logger, out bool shouldAppend) {
        Enabled = Logger.IsEnabled(LogLevel.Warn);
        shouldAppend = Enabled;
        message = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
    }

    public void AppendLiteral(string value) => message.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => message.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string? format) => message.AppendFormatted(value, format);
    public void AppendFormatted<T>(T value, int alignment) => message.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format) => message.AppendFormatted(value, alignment, format);
    public void AppendFormatted(ReadOnlySpan<char> value) => message.AppendFormatted(value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) => message.AppendFormatted(value, alignment);
    internal string GetMessage() => message.ToStringAndClear();
}

[InterpolatedStringHandler]
public ref struct ErrorLogMessageHandler {
    private DefaultInterpolatedStringHandler message;
    internal bool Enabled;

    public ErrorLogMessageHandler(int literalLength, int formattedCount, ScopedLogger logger, out bool shouldAppend) {
        Enabled = Logger.IsEnabled(LogLevel.Error);
        shouldAppend = Enabled;
        message = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
    }

    public void AppendLiteral(string value) => message.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => message.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string? format) => message.AppendFormatted(value, format);
    public void AppendFormatted<T>(T value, int alignment) => message.AppendFormatted(value, alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format) => message.AppendFormatted(value, alignment, format);
    public void AppendFormatted(ReadOnlySpan<char> value) => message.AppendFormatted(value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment) => message.AppendFormatted(value, alignment);
    internal string GetMessage() => message.ToStringAndClear();
}
