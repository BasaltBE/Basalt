using BasaltBE.Plugins;

namespace BasaltBE.Cli.Commands;

internal static class BuildCommand {
    internal static void Execute(string[] args) {
        string? project = null;
        string? output = null;
        string? runtime = null;
        bool restore = true;
        for (int index = 0; index < args.Length; index++) {
            string option = args[index];
            if (option == "--no-restore") {
                restore = false;
                continue;
            }
            if (++index == args.Length) throw new ArgumentException($"Missing value for {option}.");

            switch (option) {
                case "--project":
                    project = args[index];
                    break;
                case "--output":
                    output = args[index];
                    break;
                case "--runtime":
                    runtime = args[index];
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {option}.");
            }
        }

        using CancellationTokenSource cancellation = new();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try {
            string archive = PluginBuilder.Build(project, output, runtime, restore, cancellation.Token);
            Console.WriteLine($"Package: {archive}");
        } finally {
            Console.CancelKeyPress -= cancel;
        }
    }
}
