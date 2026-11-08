using System.Reflection;
using System.Text.Json;
using BasaltBE.Cli.Commands;

namespace BasaltBE.Cli;

internal static class Program {
    internal static int Main(string[] args) {
        try {
            if (args is ["--version"]) {
                Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString(3));
                return 0;
            }
            if (args.Length == 0 || args is ["--help"]) {
                Console.WriteLine("basalt plugin new <name> [--output <directory>] [--api-version <version>] [--author <name>] [--contributor <name>]");
                Console.WriteLine("basalt plugin build [--project <file>] [--output <directory>] [--runtime <rid>] [--no-restore]");
                Console.WriteLine("basalt plugin inspect <package>");
                return 0;
            }
            if (args.Length < 2 || args[0] != "plugin") throw new ArgumentException("Unknown command. Use basalt --help.");
            string[] tail = args[2..];
            switch (args[1]) {
                case "new":
                    NewCommand.Execute(tail);
                    break;
                case "build":
                    BuildCommand.Execute(tail);
                    break;
                case "inspect":
                    InspectCommand.Execute(tail);
                    break;
                default:
                    throw new ArgumentException("Unknown command or invalid arguments. Use basalt --help.");
            }

            return 0;
        } catch (OperationCanceledException) {
            Console.Error.WriteLine("Build canceled.");
            return 130;
        } catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or
            JsonException or ReflectionTypeLoadException or BadImageFormatException or System.ComponentModel.Win32Exception or TypeLoadException) {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
