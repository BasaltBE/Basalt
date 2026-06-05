using System.Text;

namespace Basalt;

public static class Logger
{
    private static readonly Lock Sync = new();
    private static bool IsInitialized;

    public static void Init()
    {
        lock (Sync)
        {
            if (IsInitialized)
            {
                return;
            }

            Console.OutputEncoding = Encoding.UTF8;
            IsInitialized = true;
        }
    }

    public static void Deinit()
    {
        lock (Sync)
        {
            IsInitialized = false;
        }
    }

    public static void Debug(string Format, params object?[] Args) => Log(LogLevel.Debug, Format, Args);
    public static void Info(string Format, params object?[] Args) => Log(LogLevel.Info, Format, Args);
    public static void Warn(string Format, params object?[] Args) => Log(LogLevel.Warn, Format, Args);
    public static void Err(string Format, params object?[] Args) => Log(LogLevel.Err, Format, Args);
    public static void Chat(string Format, params object?[] Args) => Log(LogLevel.Chat, Format, Args);
    public static void Error (string Format, params object?[] Args) => Log(LogLevel.Err, Format, Args);

    public static void Log(LogLevel Level, string Format, params object?[] Args)
    {
        var now = DateTime.Now;
        var header = $"[{now:MM-dd HH:mm:ss}]";
        var levelText = AsText(Level);
        var message = string.Format(Format, Args);

        lock (Sync)
        {
            Console.Write(Ansi(LogColor.DarkGray));
            Console.Write(header);
            Console.Write(Ansi(LogColor.Reset));
            Console.Write(' ');
            Console.Write(Ansi(LevelColor(Level)));
            Console.Write(levelText);
            Console.Write(Ansi(LogColor.Reset));
            Console.Write(": ");

            if (Level == LogLevel.Chat)
            {
                PrintMinecraftFormatting(message);
            }
            else
            {
                Console.Write(message);
            }

            Console.WriteLine();
            Console.Write(Ansi(LogColor.Reset));
        }
    }

    private static string AsText(LogLevel Level)
    {
        return Level switch
        {
            LogLevel.Debug => "debug",
            LogLevel.Info => "info",
            LogLevel.Warn => "warning",
            LogLevel.Err => "error",
            LogLevel.Chat => "chat",
            _ => "info",
        };
    }

    private static LogColor LevelColor(LogLevel Level)
    {
        return Level switch
        {
            LogLevel.Debug => LogColor.DarkGray,
            LogLevel.Info => LogColor.Green,
            LogLevel.Warn => LogColor.Yellow,
            LogLevel.Err => LogColor.Red,
            LogLevel.Chat => LogColor.MaterialAmethyst,
            _ => LogColor.White,
        };
    }

    private static string Ansi(LogColor Color)
    {
        return Color switch
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

    private static void PrintMinecraftFormatting(string Text)
    {
        var index = 0;
        while (index < Text.Length)
        {
            var markerLength = SectionMarkerLen(Text, index);
            if (markerLength != 0 && index + markerLength < Text.Length)
            {
                var code = char.ToLowerInvariant(Text[index + markerLength]);
                var ansiCode = MinecraftAnsiCode(code);
                if (ansiCode is not null)
                {
                    Console.Write(ansiCode);
                }

                index += markerLength + 1;
                continue;
            }

            Console.Write(Text[index]);
            index++;
        }

        Console.Write(Ansi(LogColor.Reset));
    }

    private static int SectionMarkerLen(string Text, int Index)
    {
        if (Index < Text.Length && Text[Index] == '\u00A7') return 1;
        if (Index + 1 < Text.Length && Text[Index] == '\u00C2' && Text[Index + 1] == '\u00A7') return 2;
        if (Index + 3 < Text.Length && Text[Index] == '\u00C3' && Text[Index + 1] == '\u0082' && Text[Index + 2] == '\u00C2' && Text[Index + 3] == '\u00A7') return 4;
        if (Index + 4 < Text.Length && Text[Index] == '\u00E2' && Text[Index + 1] == '\u0094' && Text[Index + 2] == '\u00AC' && Text[Index + 3] == '\u00C2' && Text[Index + 4] == '\u00BA') return 5;
        return 0;
    }

    private static string? MinecraftAnsiCode(char Code)
    {
        return Code switch
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
