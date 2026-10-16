using System.Text;
using System.Threading.Channels;
using BasaltBE.Logging.Enums;
using BasaltBE.Logging.Types;

namespace BasaltBE.Logging;

public static class Logger {
    private static readonly Channel<LogEntry> Entries = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(4096) {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    private static readonly Thread OutputThread = new(WriteEntries) {
        IsBackground = true,
        Name = "LogWriter"
    };
    private static int minimumLevel = (int)LogLevel.Info;
    private static int includeTimestamp;
    private static int stopped;
    private static long droppedEntries;

    static Logger() {
        OutputThread.Start();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
    }

    public static LogLevel MinimumLevel {
        get => (LogLevel)Volatile.Read(ref minimumLevel);
        set => Volatile.Write(ref minimumLevel, (int)value);
    }

    public static bool IncludeTimestamp {
        get => Volatile.Read(ref includeTimestamp) != 0;
        set => Volatile.Write(ref includeTimestamp, value ? 1 : 0);
    }

    public static ScopedLogger Scoped(string scope) => new(scope);

    internal static bool IsEnabled(LogLevel level) =>
        Volatile.Read(ref stopped) == 0 && level >= MinimumLevel;

    internal static void Write(LogLevel level, string scope, string message) {
        if (Volatile.Read(ref stopped) != 0) return;
        if (level < MinimumLevel) return;

        DateTime? timestamp = IncludeTimestamp ? DateTime.Now : null;
        LogEntry entry = new(level, scope, message, timestamp);
        if (Entries.Writer.TryWrite(entry)) return;

        if (level >= LogLevel.Warn && Volatile.Read(ref stopped) == 0) {
            try {
                Entries.Writer.WriteAsync(entry).AsTask().GetAwaiter().GetResult();
                return;
            }
            catch (ChannelClosedException) {
                return;
            }
        }

        if (Volatile.Read(ref stopped) == 0)
            Interlocked.Increment(ref droppedEntries);
    }

    public static void Flush() {
        if (Volatile.Read(ref stopped) != 0) {
            OutputThread.Join();
            return;
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEntry entry = new(LogLevel.Info, string.Empty, string.Empty, null, completion);
        try {
            Entries.Writer.WriteAsync(entry).AsTask().GetAwaiter().GetResult();
            completion.Task.GetAwaiter().GetResult();
        }
        catch (ChannelClosedException) {
            OutputThread.Join();
        }
    }

    public static void Shutdown() {
        if (Interlocked.Exchange(ref stopped, 1) == 0)
            Entries.Writer.TryComplete();

        OutputThread.Join();
    }

    private static void WriteEntries() {
        StringBuilder line = new(256);
        while (Entries.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult()) {
            while (Entries.Reader.TryRead(out LogEntry entry)) {
                if (entry.Completion is not null) {
                    WriteDroppedEntries(line);
                    entry.Completion.TrySetResult();
                    continue;
                }

                WriteLine(line, entry);
            }
        }

        WriteDroppedEntries(line);
    }

    private static void WriteDroppedEntries(StringBuilder line) {
        long dropped = Interlocked.Exchange(ref droppedEntries, 0);
        if (dropped != 0)
            WriteLine(line, new LogEntry(LogLevel.Warn, "Logger", $"Dropped {dropped} log entries because the queue was full.", IncludeTimestamp ? DateTime.Now : null));
    }

    private static void WriteLine(StringBuilder line, LogEntry entry) {
        line.Clear();
        line.Append(LevelColor(entry.Level));
        line.Append(LevelName(entry.Level));
        line.Append("§8(");
        line.Append("§8");
        if (entry.Timestamp is DateTime timestamp) {
            line.Append(timestamp.ToString("HH:mm:ss"));
            line.Append("§8:");
            line.Append("§7");
        }
        line.Append(entry.Scope);
        line.Append("§8): §7");
        AppendMinecraftText(line, entry.Message);

        try {
            Console.Out.WriteLine(Console.IsOutputRedirected ? StripMinecraftCodes(line) : TranslateMinecraftCodes(line));
        }
        catch {
        }
    }

    private static string StripMinecraftCodes(StringBuilder line) {
        StringBuilder output = new(line.Length);
        for (int index = 0; index < line.Length; index++) {
            if (line[index] == '§' && index + 1 < line.Length && IsMinecraftCode(line[index + 1])) {
                index++;
                continue;
            }

            output.Append(line[index]);
        }

        return output.ToString();
    }

    private static string TranslateMinecraftCodes(StringBuilder line) {
        StringBuilder output = new(line.Length + 32);
        for (int index = 0; index < line.Length; index++) {
            if (line[index] == '§' && index + 1 < line.Length && TryGetAnsi(line[index + 1], out string? ansi)) {
                output.Append(ansi);
                index++;
                continue;
            }

            output.Append(line[index]);
        }

        output.Append("\x1b[0m");
        return output.ToString();
    }

    private static void AppendMinecraftText(StringBuilder line, string message) {
        foreach (char character in message) {
            if (character == '\r') continue;
            line.Append(character);
        }
    }

    private static string LevelColor(LogLevel level) => level switch {
        LogLevel.Debug => "§8",
        LogLevel.Info => "§a",
        LogLevel.Warn => "§e",
        LogLevel.Error => "§c",
        _ => "§f"
    };

    private static string LevelName(LogLevel level) => level switch {
        LogLevel.Debug => "debug",
        LogLevel.Info => "info",
        LogLevel.Warn => "warn",
        LogLevel.Error => "error",
        _ => "info"
    };

    private static bool IsMinecraftCode(char code) => TryGetAnsi(code, out _);

    private static bool TryGetAnsi(char code, out string? ansi) {
        ansi = char.ToLowerInvariant(code) switch {
            '0' => "\x1b[30m",
            '1' => "\x1b[34m",
            '2' => "\x1b[32m",
            '3' => "\x1b[36m",
            '4' => "\x1b[31m",
            '5' => "\x1b[35m",
            '6' => "\x1b[33m",
            '7' => "\x1b[38;2;170;170;170m",
            '8' => "\x1b[38;2;85;85;85m",
            '9' => "\x1b[94m",
            'a' => "\x1b[92m",
            'b' => "\x1b[96m",
            'c' => "\x1b[91m",
            'd' => "\x1b[95m",
            'e' => "\x1b[93m",
            'f' => "\x1b[97m",
            'w' => "\x1b[38;2;139;179;255m",
            'k' => "",
            'l' => "\x1b[1m",
            'm' => "\x1b[9m",
            'n' => "\x1b[4m",
            'o' => "\x1b[3m",
            'r' => "\x1b[0m",
            _ => null
        };

        return ansi is not null;
    }
}


