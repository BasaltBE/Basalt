namespace BasaltBE.Plugins.Packaging;

public sealed class PackageDescriptor {
    public int FormatVersion = 1;
    public string EntryAssembly = "";
    public string EntryType = "";
    public string TargetFramework = "net10.0";
    public string? RuntimeIdentifier;
    public string[] HostAssemblies = [];
}
