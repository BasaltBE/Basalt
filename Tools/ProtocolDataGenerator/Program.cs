using System.Text;

if (args.Length != 2) {
    throw new ArgumentException("Expected the data directory and output path.");
}

string dataDirectory = Path.GetFullPath(args[0]);
string outputPath = Path.GetFullPath(args[1]);
string outputDirectory = Path.GetDirectoryName(outputPath)
    ?? throw new ArgumentException("The output path must include a directory.", nameof(args));

string[] files = Directory.GetFiles(dataDirectory, "*.json", SearchOption.AllDirectories)
    .OrderBy(static path => path, StringComparer.Ordinal)
    .ToArray();

Directory.CreateDirectory(outputDirectory);
using FileStream stream = File.Create(outputPath);
using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: false);

writer.Write(Encoding.ASCII.GetBytes("BASDATA1"));
writer.Write(files.Length);

for (int i = 0; i < files.Length; i++) {
    string name = Path.GetRelativePath(dataDirectory, files[i]).Replace(Path.DirectorySeparatorChar, '/');
    byte[] nameBytes = Encoding.UTF8.GetBytes(name);
    byte[] data = File.ReadAllBytes(files[i]);

    writer.Write(nameBytes.Length);
    writer.Write(nameBytes);
    writer.Write(data.Length);
    writer.Write(data);
}
