namespace Basalt.Core.Resources;

using System.Security.Cryptography;

/// <summary>
/// Represents a loaded resource pack with its manifest metadata and compressed data.
/// </summary>
public sealed class ResourcePack
{
    public required string FolderName { get; init; }
    public required Guid Uuid { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int[] Version { get; init; }
    public required byte[] Data { get; init; }
    public required byte[] Hash { get; init; }

    public string VersionString => $"{Version[0]}.{Version[1]}.{Version[2]}";

    public ulong Size => (ulong)Data.Length;

    public uint ChunkCount(uint chunkSize)
    {
        if (chunkSize == 0) return 0;
        return (uint)((Data.Length + chunkSize - 1) / chunkSize);
    }

    public static ResourcePack Create(string folderName, Guid uuid, string name, string description, int[] version, byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return new ResourcePack
        {
            FolderName = folderName,
            Uuid = uuid,
            Name = name,
            Description = description,
            Version = version.Length >= 3 ? version : [0, 0, 0],
            Data = data,
            Hash = hash
        };
    }
}
