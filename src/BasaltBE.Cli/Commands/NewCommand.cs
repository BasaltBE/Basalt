using System.Security;
using System.Text.Json;
using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Cli.Commands;

internal static class NewCommand {
    internal static void Execute(string[] args) {
        if (args.Length == 0 || args[0].StartsWith('-'))
            throw new ArgumentException("Use basalt plugin new <name>.");
        string name = args[0];
        string output = name;
        string apiPackage = "1.0.0";
        List<string> authors = [];
        List<string> contributors = [];
        for (int index = 1; index < args.Length; index++)
        {
            string option = args[index];
            if (++index == args.Length)
                throw new ArgumentException($"Missing value for {option}.");
            switch (option)
            {
                case "--output":
                    output = args[index];
                    break;
                case "--api-version":
                    apiPackage = args[index];
                    break;
                case "--author":
                    authors.Add(args[index]);
                    break;
                case "--contributor":
                    contributors.Add(args[index]);
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {option}.");
            }
        }

        if (!Version.TryParse(apiPackage, out Version? version) || version.Build < 0 || version.Revision >= 0)
            throw new ArgumentException("API package version must have three numeric parts.");

        PluginManifest manifest = new() {
            Id = string.Concat(name.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')).ToLowerInvariant(),
            Name = name,
            Version = "1.0.0",
            Api = version.Major.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Authors = authors.ToArray(),
            Contributors = contributors.ToArray()
        };
        manifest.Validate();
        string directory = Path.GetFullPath(output);
        if (File.Exists(directory) || Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException($"Output directory is not empty: {directory}.");
        Directory.CreateDirectory(directory);


        string project = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>{{SecurityElement.Escape(name)}}</AssemblyName>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <EnableDynamicLoading>true</EnableDynamicLoading>
                <Deterministic>true</Deterministic>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="BasaltBE.Plugins" Version="{{apiPackage}}" />
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(directory, name + ".csproj"), project);
        File.WriteAllText(Path.Combine(directory, "plugin.json"), JsonSerializer.Serialize(manifest, PluginManifest.Json));
        File.WriteAllText(Path.Combine(directory, "ExamplePlugin.cs"), """
            using BasaltBE.Plugins;

            namespace Example;

            public sealed class ExamplePlugin : Plugin {
                public override void OnEnable() {
                    Logger.Info("Plugin enabled.");
                }
            }
            """);
        File.WriteAllText(Path.Combine(directory, "README.md"), $"# {name}\n\nBuild with `basalt plugin build`.\n");
        Console.WriteLine($"Created {name} in {directory}.");
    }
}
