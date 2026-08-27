namespace Basalt.Tests;

using System.Buffers.Binary;
using System.Text;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.BedrockProtocol.Types;

public sealed class LevelDbMigrationTests {
    [Fact]
    public async Task ConcurrentReadsAndWritesRemainConsistent() {
        string path = Path.Combine(Path.GetTempPath(), $"basalt-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try {
            using LevelDbDatabase database = new(Path.Combine(path, "db"));
            Task[] operations = Enumerable.Range(0, 64).Select(index => Task.Run(() => {
                byte[] key = Encoding.UTF8.GetBytes($"concurrent_{index}");
                byte[] value = BitConverter.GetBytes(index);
                database.Put(key, value);
                Assert.Equal(value, database.Get(key));
            })).ToArray();

            await Task.WhenAll(operations);
        }
        finally {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        }
    }

    [Fact]
    public void LegacySpawnPositionMigratesToTheCurrentKey() {
        string path = Path.Combine(Path.GetTempPath(), $"basalt-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try {
            byte[] value = new byte[12];
            BinaryPrimitives.WriteSingleLittleEndian(value.AsSpan(0, 4), 12.5f);
            BinaryPrimitives.WriteSingleLittleEndian(value.AsSpan(4, 4), 80f);
            BinaryPrimitives.WriteSingleLittleEndian(value.AsSpan(8, 4), -3.25f);

            using (LevelDbDatabase database = new(Path.Combine(path, "db"))) {
                database.Put(
                    LevelDbKeyBuilder.BuildLegacySpawnPositionKey(DimensionId.Overworld),
                    value);
            }

            using (LevelDbProvider provider = new(path)) {
                Vec3? position = provider.LoadSpawnPosition(DimensionId.Overworld);
                Assert.NotNull(position);
                Assert.Equal(12.5f, position!.X);
                Assert.Equal(80f, position.Y);
                Assert.Equal(-3.25f, position.Z);
            }

            using LevelDbDatabase migrated = new(Path.Combine(path, "db"));
            Assert.NotNull(migrated.Get(LevelDbKeyBuilder.BuildSpawnPositionKey(DimensionId.Overworld)));
            Assert.Null(migrated.Get(LevelDbKeyBuilder.BuildLegacySpawnPositionKey(DimensionId.Overworld)));
        }
        finally {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
