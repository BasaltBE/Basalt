using System.Text;

internal static class ProtocolDataGenerator
{
    public static void Generate(string dataDirectoryPath, string outputPath)
    {
        string dataDirectory = Path.GetFullPath(dataDirectoryPath);
        string outputFile = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(outputFile)
            ?? throw new ArgumentException("The output path must include a directory.", nameof(outputPath));

        string[] files = Directory.GetFiles(dataDirectory, "*.json", SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Directory.CreateDirectory(outputDirectory);
        using FileStream stream = File.Create(outputFile);
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: false);

        writer.Write(Encoding.ASCII.GetBytes("BASDATA1"));
        writer.Write(files.Length);

        foreach (string file in files)
        {
            string name = Path.GetRelativePath(dataDirectory, file)
                .Replace(Path.DirectorySeparatorChar, '/');
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] data = File.ReadAllBytes(file);

            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(data.Length);
            writer.Write(data);
        }
    }
}
