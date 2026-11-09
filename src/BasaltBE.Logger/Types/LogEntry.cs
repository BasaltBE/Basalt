using BasaltBE.Logging.Enums;

namespace BasaltBE.Logging.Types;

internal readonly record struct LogEntry(
    LogLevel Level,
    string Scope,
    string Message,
    DateTime? Timestamp,
    TaskCompletionSource? Completion = null);

