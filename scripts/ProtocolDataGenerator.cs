using System.Text;
using System.Security.Cryptography;

internal static class ProtocolDataGenerator
{
    public static bool Generate(string dataDirectoryPath, string outputPath, string hashPath)
    {
        string dataDirectory = Path.GetFullPath(dataDirectoryPath);
        string outputFile = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(outputFile)
            ?? throw new ArgumentException("The output path must include a directory.", nameof(outputPath));

        string[] files = Directory.GetFiles(dataDirectory, "*.json", SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        string hash = ComputeHash(dataDirectory, files);
        if (File.Exists(outputFile) && File.Exists(hashPath) &&
            string.Equals(File.ReadAllText(hashPath), hash, StringComparison.Ordinal))
        {
            Console.WriteLine($"Protocol JSON unchanged. Reuse embedded data ({files.Length} files).");
            return false;
        }

        DateTime generatedAtUtc = DateTime.UtcNow;
        Console.WriteLine($"Protocol JSON changed. Embedding {files.Length} files from '{dataDirectory}'.");

        Directory.CreateDirectory(outputDirectory);
        using FileStream stream = File.Create(outputFile);
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: false);

        writer.Write(Encoding.ASCII.GetBytes("BASDATA2"));
        writer.Write(generatedAtUtc.Ticks);
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

        stream.Flush(true);
        File.SetLastWriteTimeUtc(outputFile, generatedAtUtc);

        File.WriteAllText(hashPath, hash, Encoding.ASCII);
        return true;
    }

    private static string ComputeHash(string dataDirectory, string[] files)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData("BASDATA2"u8);

        foreach (string file in files)
        {
            string name = Path.GetRelativePath(dataDirectory, file)
                .Replace(Path.DirectorySeparatorChar, '/');
            hash.AppendData(Encoding.UTF8.GetBytes(name));
            hash.AppendData(File.ReadAllBytes(file));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
