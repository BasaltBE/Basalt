namespace Basalt.Core;

using System.Buffers.Binary;
using System.Reflection;
using System.Text;

/// <summary>
/// Provides access to embedded Protocol/Data JSON resources.
/// </summary>
internal static class ProtocolData {
    private const int HeaderSize = 20;
    private static readonly Assembly DataAssembly = typeof(ProtocolData).Assembly;
    private static readonly Lazy<Dictionary<string, (int Offset, int Length)>> Sections = new(LoadSections);
    private static readonly Lazy<byte[]> Data = new(LoadData);

    public static DateTime GeneratedAtUtc {
        get {
            ReadOnlySpan<byte> source = Data.Value;
            if (source.Length < HeaderSize || !source[..8].SequenceEqual("BASDATA2"u8)) {
                throw new InvalidDataException("The embedded protocol data header is invalid.");
            }

            return new DateTime(BinaryPrimitives.ReadInt64LittleEndian(source[8..]), DateTimeKind.Utc);
        }
    }

    public static Stream? Open(string fileName) {
        string normalizedName = fileName.Replace('\\', '/');
        if (!Sections.Value.TryGetValue(normalizedName, out (int Offset, int Length) section)) {
            normalizedName = normalizedName.Replace('_', '-');
            if (!Sections.Value.TryGetValue(normalizedName, out section)) {
                return null;
            }
        }

        return new MemoryStream(Data.Value, section.Offset, section.Length, writable: false, publiclyVisible: true);
    }

    public static Stream Require(string fileName) {
        return Open(fileName)
          ?? throw new FileNotFoundException($"Embedded protocol data '{fileName}' not found.");
    }

    private static byte[] LoadData() {
        using Stream stream = DataAssembly.GetManifestResourceStream("BedrockProtocol.Data.protocol_data.bin")
            ?? throw new FileNotFoundException("Embedded protocol data resource was not found.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static Dictionary<string, (int Offset, int Length)> LoadSections() {
        byte[] data = Data.Value;
        ReadOnlySpan<byte> source = data;
        if (source.Length < HeaderSize || !source[..8].SequenceEqual("BASDATA2"u8)) {
            throw new InvalidDataException("The embedded protocol data header is invalid.");
        }

        int offset = 8;
        _ = ReadInt64(source, ref offset);
        int count = ReadInt32(source, ref offset);
        Dictionary<string, (int Offset, int Length)> sections = new(count, StringComparer.Ordinal);

        for (int i = 0; i < count; i++) {
            string name = ReadString(source, ref offset);
            int length = ReadInt32(source, ref offset);
            if (length < 0 || length > source.Length - offset) {
                throw new InvalidDataException("The embedded protocol data section is invalid.");
            }

            sections[name] = (offset, length);
            offset += length;
        }

        return sections;
    }

    private static int ReadInt32(ReadOnlySpan<byte> source, ref int offset) {
        if (source.Length - offset < sizeof(int)) {
            throw new InvalidDataException("The embedded protocol data is truncated.");
        }

        int value = BinaryPrimitives.ReadInt32LittleEndian(source[offset..]);
        offset += sizeof(int);
        return value;
    }

    private static long ReadInt64(ReadOnlySpan<byte> source, ref int offset) {
        if (source.Length - offset < sizeof(long)) {
            throw new InvalidDataException("The embedded protocol data is truncated.");
        }

        long value = BinaryPrimitives.ReadInt64LittleEndian(source[offset..]);
        offset += sizeof(long);
        return value;
    }

    private static string ReadString(ReadOnlySpan<byte> source, ref int offset) {
        int length = ReadInt32(source, ref offset);
        if (length < 0 || length > source.Length - offset) {
            throw new InvalidDataException("The embedded protocol data string is invalid.");
        }

        string value = Encoding.UTF8.GetString(source.Slice(offset, length));
        offset += length;
        return value;
    }
}
