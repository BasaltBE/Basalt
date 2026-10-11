using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BasaltBE.Basalt.Network.ResourcePacks;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class ResourcePackManagerTests {
    [Fact]
    public void LoadBuildsArchiveAndServesChunksFromCachedBytes() {
        string directory = Path.Combine(Path.GetTempPath(), $"BasaltBE.ResourcePackTests.{Guid.NewGuid():N}");
        string packDirectory = Path.Combine(directory, "test-pack");
        Directory.CreateDirectory(packDirectory);
        try {
            Guid id = Guid.NewGuid();
            byte[] asset = new byte[300_000];
            new Random(42).NextBytes(asset);
            File.WriteAllText(Path.Combine(packDirectory, "manifest.json"), $$"""
                {
                  "header": {
                    "uuid": "{{id}}",
                    "name": "Test Pack",
                    "description": "Resource pack test",
                    "version": [1, 2, 3]
                  }
                }
                """);
            File.WriteAllBytes(Path.Combine(packDirectory, "asset.bin"), asset);

            ResourcePackManager manager = ResourcePackManager.Load(directory);

            Assert.False(manager.Required);
            Assert.Single(manager.Packs);
            Assert.True(File.Exists(Path.Combine(directory, "config.json")));
            ResourcePack pack = manager.Packs[0];
            Assert.Equal(id, pack.Id);
            Assert.Equal("1.2.3", pack.Version);
            Assert.True(manager.TryGet($"{id}_1.2.3", out ResourcePack? found));
            Assert.Same(pack, found);

            uint chunkCount = pack.GetChunkCount(ResourcePackManager.ChunkSize);
            Assert.True(chunkCount > 1);
            using MemoryStream assembled = new(pack.ArchiveLength);
            for (uint chunkId = 0; chunkId < chunkCount; chunkId++) {
                ReadOnlyMemory<byte> chunk = pack.GetChunk(chunkId, ResourcePackManager.ChunkSize);
                Assert.True(MemoryMarshal.TryGetArray(chunk, out ArraySegment<byte> segment));
                Assert.Same(pack.Archive, segment.Array);
                assembled.Write(chunk.Span);
            }

            byte[] archive = assembled.ToArray();
            Assert.Equal(pack.ArchiveLength, archive.Length);
            Assert.Equal(pack.Hash, SHA256.HashData(archive));
            using ZipArchive zip = new(new MemoryStream(archive), ZipArchiveMode.Read);
            ZipArchiveEntry assetEntry = Assert.Single(zip.Entries, entry => entry.FullName == "asset.bin");
            using Stream assetStream = assetEntry.Open();
            using MemoryStream unpackedAsset = new();
            assetStream.CopyTo(unpackedAsset);
            Assert.Equal(asset, unpackedAsset.ToArray());

            for (int i = 0; i < 100; i++)
                pack.GetChunk((uint)(i % chunkCount), ResourcePackManager.ChunkSize);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10_000; i++)
                pack.GetChunk((uint)(i % chunkCount), ResourcePackManager.ChunkSize);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);

            Assert.Throws<ArgumentOutOfRangeException>(() => pack.GetChunk(chunkCount, ResourcePackManager.ChunkSize));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadUsesConfigurationToSelectPacksAndRequireThem() {
        string directory = Path.Combine(Path.GetTempPath(), $"BasaltBE.ResourcePackTests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try {
            Guid enabledId = AddPack(directory, "enabled", "Enabled");
            AddPack(directory, "disabled", "Disabled");
            File.WriteAllText(Path.Combine(directory, "config.json"), $$"""
                {
                  "required": true,
                  "enabledPacks": ["{{enabledId}}"]
                }
                """);

            ResourcePackManager manager = ResourcePackManager.Load(directory);

            Assert.True(manager.Required);
            Assert.Single(manager.Packs);
            Assert.Equal(enabledId, manager.Packs[0].Id);
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Guid AddPack(string directory, string name, string packName) {
        string packDirectory = Path.Combine(directory, name);
        Directory.CreateDirectory(packDirectory);
        Guid id = Guid.NewGuid();
        File.WriteAllText(Path.Combine(packDirectory, "manifest.json"), $$"""
            {
              "header": {
                "uuid": "{{id}}",
                "name": "{{packName}}",
                "description": "Test pack",
                "version": [1, 0, 0]
              }
            }
            """);
        File.WriteAllText(Path.Combine(packDirectory, "content.txt"), packName);
        return id;
    }
}
