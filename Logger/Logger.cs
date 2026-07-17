using System.Text;

namespace Basalt;

public static class Logger
{
    private static readonly Lock Sync = new();
    private static bool _isInitialized;

    [ThreadStatic]
    private static StringBuilder? _builder;

    public enum LogLevel { Debug, Info, Warn, Err, Chat }

    public static LogLevel MinLevel { get; set; } = LogLevel.Info;

    public static void Init()
    {
        lock (Sync)
        {
            if (_isInitialized)
            {
                return;
            }

            Console.OutputEncoding = Encoding.UTF8;
            _isInitialized = true;
        }
    }

    public static void Deinit()
    {
        lock (Sync)
        {
            _isInitialized = false;
        }
    }

    public static void Debug(string format, params object?[] args) => Log(LogLevel.Debug, format, args);
    public static void Info(string format, params object?[] args) => Log(LogLevel.Info, format, args);
    public static void Warn(string format, params object?[] args) => Log(LogLevel.Warn, format, args);
    public static void Err(string format, params object?[] args) => Log(LogLevel.Err, format, args);
    public static void Chat(string format, params object?[] args) => Log(LogLevel.Chat, format, args);
    public static void Error(string format, params object?[] args) => Log(LogLevel.Err, format, args);

    public static void Log(LogLevel level, string format, params object?[] args)
    {
        if (level < MinLevel && level != LogLevel.Chat)
        {
            return;
        }

        var message = args.Length > 0 ? string.Format(format, args) : format;
        var sb = GetBuilder();

        sb.Append(Ansi(LogColor.DarkGray));
        sb.Append('[');
        sb.Append(DateTime.Now.ToString("HH:mm:ss"));
        sb.Append(']');
        sb.Append(Ansi(LogColor.Reset));
        sb.Append(' ');
        sb.Append(Ansi(LevelColor(level)));
        sb.Append(AsText(level));
        sb.Append(Ansi(LogColor.Reset));
        sb.Append(": ");

        if (level == LogLevel.Chat)
        {
            AppendMinecraftFormatting(sb, message);
        }
        else
        {
            sb.Append(message);
        }

        sb.AppendLine();
        sb.Append(Ansi(LogColor.Reset));

        var output = sb.ToString();
        sb.Clear();

        lock (Sync)
        {
            Console.Write(output);
        }
    }

    private static StringBuilder GetBuilder()
    {
        _builder ??= new StringBuilder(256);
        return _builder;
    }

    private static string AsText(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "debug",
            LogLevel.Info => "info",
            LogLevel.Warn => "warning",
            LogLevel.Err => "error",
            LogLevel.Chat => "chat",
            _ => "info",
        };
    }

    private static LogColor LevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => LogColor.DarkGray,
            LogLevel.Info => LogColor.Green,
            LogLevel.Warn => LogColor.Yellow,
            LogLevel.Err => LogColor.Red,
            LogLevel.Chat => LogColor.MaterialAmethyst,
            _ => LogColor.White,
        };
    }

    private static string Ansi(LogColor color)
    {
        return color switch
        {
            LogColor.Black => "\x1b[30m",
            LogColor.DarkBlue => "\x1b[34m",
            LogColor.DarkGreen => "\x1b[32m",
            LogColor.DarkAqua => "\x1b[36m",
            LogColor.DarkRed => "\x1b[31m",
            LogColor.DarkPurple => "\x1b[35m",
            LogColor.Gold => "\x1b[33m",
            LogColor.Gray => "\x1b[37m",
            LogColor.DarkGray => "\x1b[90m",
            LogColor.Blue => "\x1b[94m",
            LogColor.Green => "\x1b[92m",
            LogColor.Aqua => "\x1b[96m",
            LogColor.Red => "\x1b[91m",
            LogColor.LightPurple => "\x1b[95m",
            LogColor.Yellow => "\x1b[93m",
            LogColor.White => "\x1b[97m",
            LogColor.MinecoinGold => "\x1b[93m",
            LogColor.MaterialQuartz => "\x1b[37m",
            LogColor.MaterialIron => "\x1b[37m",
            LogColor.MaterialNetherite => "\x1b[90m",
            LogColor.MaterialRedstone => "\x1b[91m",
            LogColor.MaterialCopper => "\x1b[33m",
            LogColor.MaterialGold => "\x1b[93m",
            LogColor.MaterialEmerald => "\x1b[92m",
            LogColor.MaterialDiamond => "\x1b[96m",
            LogColor.MaterialLapis => "\x1b[34m",
            LogColor.MaterialAmethyst => "\x1b[95m",
            _ => "\x1b[0m",
        };
    }

    private static void AppendMinecraftFormatting(StringBuilder sb, string text)
    {
        var index = 0;
        var spanStart = 0;

        while (index < text.Length)
        {
            var markerLength = SectionMarkerLen(text, index);
            if (markerLength != 0 && index + markerLength < text.Length)
            {
                if (index > spanStart)
                {
                    sb.Append(text, spanStart, index - spanStart);
                }

                var code = char.ToLowerInvariant(text[index + markerLength]);
                var ansiCode = MinecraftAnsiCode(code);
                if (ansiCode is not null)
                {
                    sb.Append(ansiCode);
                }

                index += markerLength + 1;
                spanStart = index;
                continue;
            }

            index++;
        }

        if (index > spanStart)
        {
            sb.Append(text, spanStart, index - spanStart);
        }

        sb.Append(Ansi(LogColor.Reset));
    }

    private static int SectionMarkerLen(string text, int index)
    {
        if (index < text.Length && text[index] == '\u00A7')
            return 1;

        if (index + 1 < text.Length && text[index] == '\u00C2' && text[index + 1] == '\u00A7')
            return 2;

        if (index + 3 < text.Length
          && text[index] == '\u00C3' && text[index + 1] == '\u0082'
          && text[index + 2] == '\u00C2' && text[index + 3] == '\u00A7')
            return 4;

        if (index + 4 < text.Length
          && text[index] == '\u00E2' && text[index + 1] == '\u0094'
          && text[index + 2] == '\u00AC' && text[index + 3] == '\u00C2'
          && text[index + 4] == '\u00BA')
            return 5;

        return 0;
    }

    private static string? MinecraftAnsiCode(char code)
    {
        return code switch
        {
            '0' => Ansi(LogColor.Black),
            '1' => Ansi(LogColor.DarkBlue),
            '2' => Ansi(LogColor.DarkGreen),
            '3' => Ansi(LogColor.DarkAqua),
            '4' => Ansi(LogColor.DarkRed),
            '5' => Ansi(LogColor.DarkPurple),
            '6' => Ansi(LogColor.Gold),
            '7' => Ansi(LogColor.Gray),
            '8' => Ansi(LogColor.DarkGray),
            '9' => Ansi(LogColor.Blue),
            'a' => Ansi(LogColor.Green),
            'b' => Ansi(LogColor.Aqua),
            'c' => Ansi(LogColor.Red),
            'd' => Ansi(LogColor.LightPurple),
            'e' => Ansi(LogColor.Yellow),
            'f' => Ansi(LogColor.White),
            'g' => Ansi(LogColor.MinecoinGold),
            'h' => Ansi(LogColor.MaterialQuartz),
            'i' => Ansi(LogColor.MaterialIron),
            'j' => Ansi(LogColor.MaterialNetherite),
            'l' => "\x1b[1m",
            'm' => "\x1b[9m",
            'n' => "\x1b[4m",
            'o' => "\x1b[3m",
            'p' => Ansi(LogColor.MaterialRedstone),
            'q' => Ansi(LogColor.MaterialCopper),
            'r' => Ansi(LogColor.Reset),
            's' => Ansi(LogColor.MaterialGold),
            't' => Ansi(LogColor.MaterialEmerald),
            'u' => Ansi(LogColor.MaterialAmethyst),
            _ => null,
        };
    }
}
