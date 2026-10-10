using System.Text;
using Basalt.Binary;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.NetherNet.Packets;

public static class ServerAdvertisementCodec {
    public const byte CurrentVersion = 7;

    public static byte[] Encode(ServerAdvertisement advertisement) {
        ArgumentNullException.ThrowIfNull(advertisement);
        int nameLength = Encoding.UTF8.GetByteCount(advertisement.Name);
        int versionLength = Encoding.UTF8.GetByteCount(advertisement.Version);
        int levelLength = Encoding.UTF8.GetByteCount(advertisement.Level);
        int nonceLength = Encoding.UTF8.GetByteCount(advertisement.Nonce);
        int capacity = checked(50 + nameLength + versionLength + levelLength + nonceLength);

        using BinaryStream stream = BinaryStream.Rent(capacity);
        Basalt.Binary.BinaryWriter writer = stream.GetWriter();
        writer.WriteUInt8(CurrentVersion);
        writer.WriteVarString(advertisement.Name);
        writer.WriteZigZag(advertisement.Protocol);
        writer.WriteVarString(advertisement.Version);
        writer.WriteVarString(advertisement.Level);
        writer.WriteZigZag(advertisement.Players);
        writer.WriteZigZag(advertisement.MaxPlayers);
        writer.WriteZigZag(advertisement.GameType);
        writer.WriteBool(advertisement.EditorWorld);
        writer.WriteBool(advertisement.Hardcore);
        writer.WriteBool(advertisement.AcceptsOnlineAuth);
        writer.WriteBool(advertisement.AcceptsSelfSignedAuth);
        writer.WriteVarString(advertisement.Nonce);
        writer.WriteZigZag(advertisement.ConnectionType);
        return stream.GetProcessedBytes().ToArray();
    }
}

