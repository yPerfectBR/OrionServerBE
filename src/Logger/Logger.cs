using System.Text;
using Orion.Config;

namespace Orion.Logger;

public static class Logger
{
    private static readonly Lock Sync = new();
    private static bool _isInitialized;
    private static LogLevelConfig _levels = new();

    public static void Init()
    {
        lock (Sync)
        {
            if (_isInitialized)
            {
                return;
            }

            Console.OutputEncoding = Encoding.UTF8;

            if (OrionInfo.IsLoaded)
            {
                Configure(OrionInfo.Logging.LogLevel);
            }

            _isInitialized = true;
        }
    }

    public static void Configure(LogLevelConfig levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

        lock (Sync)
        {
            _levels = levels;
            _isInitialized = true;
            Console.OutputEncoding = Encoding.UTF8;
        }
    }

    public static void Deinit()
    {
        lock (Sync)
        {
            _isInitialized = false;
            _levels = new LogLevelConfig();
        }
    }

    public static bool IsEnabled(LogCategory category, LogLevel level)
    {
        lock (Sync)
        {
            return _levels.IsEnabled(category, level);
        }
    }

    public static void Debug(string format, params object?[] args) =>
        Debug(LogCategory.Orion, format, args);

    public static void Info(string format, params object?[] args) =>
        Info(LogCategory.Orion, format, args);

    public static void Warn(string format, params object?[] args) =>
        Warn(LogCategory.Orion, format, args);

    public static void Err(string format, params object?[] args) =>
        Error(LogCategory.Orion, format, args);

    public static void Error(string format, params object?[] args) =>
        Error(LogCategory.Orion, format, args);

    public static void Chat(string format, params object?[] args) =>
        Chat(LogCategory.Orion, format, args);

    public static void Debug(LogCategory category, string format, params object?[] args) =>
        Log(category, LogLevel.Debug, format, args);

    public static void Info(LogCategory category, string format, params object?[] args) =>
        Log(category, LogLevel.Info, format, args);

    public static void Warn(LogCategory category, string format, params object?[] args) =>
        Log(category, LogLevel.Warn, format, args);

    public static void Error(LogCategory category, string format, params object?[] args) =>
        Log(category, LogLevel.Error, format, args);

    public static void Chat(LogCategory category, string format, params object?[] args) =>
        Log(category, LogLevel.Chat, format, args);

    public static void Log(LogCategory category, LogLevel level, string format, params object?[] args)
    {
        if (!IsEnabled(category, level))
        {
            return;
        }

        DateTime now = DateTime.Now;
        string header = $"[{now:MM-dd HH:mm:ss}]";
        string levelText = AsText(level);
        string categoryText = category.ToString();
        string message = string.Format(format, args);

        lock (Sync)
        {
            Console.Write(Ansi(LogColor.DarkGray));
            Console.Write(header);
            Console.Write(Ansi(LogColor.Reset));
            Console.Write(' ');
            Console.Write(Ansi(LogColor.DarkGray));
            Console.Write('[');
            Console.Write(categoryText);
            Console.Write(']');
            Console.Write(Ansi(LogColor.Reset));
            Console.Write(' ');
            Console.Write(Ansi(LevelColor(level)));
            Console.Write(levelText);
            Console.Write(Ansi(LogColor.Reset));
            Console.Write(": ");

            if (level == LogLevel.Chat)
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

    private static string AsText(LogLevel level) => level switch
    {
        LogLevel.Debug => "debug",
        LogLevel.Info => "info",
        LogLevel.Warn => "warning",
        LogLevel.Error => "error",
        LogLevel.Chat => "chat",
        _ => "info"
    };

    private static LogColor LevelColor(LogLevel level) => level switch
    {
        LogLevel.Debug => LogColor.DarkGray,
        LogLevel.Info => LogColor.Green,
        LogLevel.Warn => LogColor.Yellow,
        LogLevel.Error => LogColor.Red,
        LogLevel.Chat => LogColor.MaterialAmethyst,
        _ => LogColor.White
    };

    private static string Ansi(LogColor color) => color switch
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
        _ => "\x1b[0m"
    };

    private static void PrintMinecraftFormatting(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int markerLength = SectionMarkerLen(text, index);
            if (markerLength != 0 && index + markerLength < text.Length)
            {
                char code = char.ToLowerInvariant(text[index + markerLength]);
                string? ansiCode = MinecraftAnsiCode(code);
                if (ansiCode is not null)
                {
                    Console.Write(ansiCode);
                }

                index += markerLength + 1;
                continue;
            }

            Console.Write(text[index]);
            index++;
        }

        Console.Write(Ansi(LogColor.Reset));
    }

    private static int SectionMarkerLen(string text, int index)
    {
        if (index < text.Length && text[index] == '\u00A7') return 1;
        if (index + 1 < text.Length && text[index] == '\u00C2' && text[index + 1] == '\u00A7') return 2;
        if (index + 3 < text.Length && text[index] == '\u00C3' && text[index + 1] == '\u0082' && text[index + 2] == '\u00C2' && text[index + 3] == '\u00A7') return 4;
        if (index + 4 < text.Length && text[index] == '\u00E2' && text[index + 1] == '\u0094' && text[index + 2] == '\u00AC' && text[index + 3] == '\u00C2' && text[index + 4] == '\u00BA') return 5;
        return 0;
    }

    private static string? MinecraftAnsiCode(char code) => code switch
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
        _ => null
    };
}
