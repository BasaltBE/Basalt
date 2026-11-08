using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Cli.Commands;

internal static class InspectCommand {
    internal static void Execute(string[] args) {
        if (args.Length != 1) throw new ArgumentException("Use basalt plugin inspect <package>.");

        PluginArchive package = new(args[0]);
        Console.WriteLine($"{package.Manifest.Name} {package.Manifest.Version}");
        Console.WriteLine($"Id: {package.Manifest.Id}");
        Console.WriteLine($"API: {package.Manifest.Api}");
        if (package.Manifest.Authors.Length != 0) Console.WriteLine($"Authors: {string.Join(", ", package.Manifest.Authors)}");
        if (package.Manifest.Contributors.Length != 0)
            Console.WriteLine($"Contributors: {string.Join(", ", package.Manifest.Contributors)}");
        Console.WriteLine($"Entry: {package.Descriptor.EntryAssembly} ({package.Descriptor.EntryType})");
        Console.WriteLine($"Runtime: {package.Descriptor.RuntimeIdentifier ?? "portable"}");
        foreach (string file in package.Files) Console.WriteLine(file);
    }
}
