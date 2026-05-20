using System.Text;
using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record LoginPacket : DataPacket
{
    public LoginPacket() { }

    public LoginPacket(int protocol, string identity, string client)
    {
        Protocol = protocol;
        Identity = identity;
        Client = client;
    }

    public int Protocol { get; set; }
    public string Identity { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;

    public override PacketId PacketId => PacketId.Login;

    public override void Deserialize(BinaryReader reader)
    {
        Protocol = reader.ReadInt32(false);

        int connectionRequestLength = checked((int)reader.ReadVarUInt());
        if (connectionRequestLength < 0 || connectionRequestLength > reader.Remaining)
        {
            throw new InvalidOperationException("Invalid login connection request length.");
        }

        int offset = 0;
        BinaryReader requestReader = new(reader.ReadBytes(connectionRequestLength), ref offset);
        Identity = requestReader.ReadString32(true);
        Client = requestReader.ReadString32(true);

        if (requestReader.Remaining != 0)
        {
            throw new InvalidOperationException("Unexpected trailing login connection request data.");
        }
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32(Protocol, false);

        int identityBytes = Encoding.UTF8.GetByteCount(Identity);
        int clientBytes = Encoding.UTF8.GetByteCount(Client);
        int connectionRequestLength = checked(sizeof(uint) + identityBytes + sizeof(uint) + clientBytes);

        writer.WriteVarUInt((uint)connectionRequestLength);
        writer.WriteString32(Identity, true);
        writer.WriteString32(Client, true);
    }
}
