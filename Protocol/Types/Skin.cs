// credit to https://github.com/Sandertv/gophertunnel/blob/master/minecraft/protocol/skin.go
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public static class SkinAnimationType
{
    public const uint Head = 1;
    public const uint Body32x32 = 2;
    public const uint Body128x128 = 3;
}

public static class SkinExpressionType
{
    public const uint Linear = 0;
    public const uint Blinking = 1;
}

public sealed class Skin
{
    public string SkinID { get; set; } = string.Empty;
    public string PlayFabID { get; set; } = string.Empty;
    public byte[] SkinResourcePatch { get; set; } = [];
    public uint SkinImageWidth { get; set; }
    public uint SkinImageHeight { get; set; }
    public byte[] SkinData { get; set; } = [];
    public List<SkinAnimation> Animations { get; set; } = [];
    public uint CapeImageWidth { get; set; }
    public uint CapeImageHeight { get; set; }
    public byte[] CapeData { get; set; } = [];
    public byte[] SkinGeometry { get; set; } = [];
    public byte[] AnimationData { get; set; } = [];
    public byte[] GeometryDataEngineVersion { get; set; } = [];
    public bool PremiumSkin { get; set; }
    public bool PersonaSkin { get; set; }
    public bool PersonaCapeOnClassicSkin { get; set; }
    public bool PrimaryUser { get; set; }
    public string CapeID { get; set; } = string.Empty;
    public string FullID { get; set; } = string.Empty;
    public string SkinColour { get; set; } = string.Empty;
    public string ArmSize { get; set; } = string.Empty;
    public List<PersonaPiece> PersonaPieces { get; set; } = [];
    public List<PersonaPieceTintColour> PieceTintColours { get; set; } = [];
    public bool Trusted { get; set; }
    public bool OverrideAppearance { get; set; }

    public void Deserialize(ref BinaryReader reader)
    {
        SkinID = reader.ReadVarString();
        PlayFabID = reader.ReadVarString();
        SkinResourcePatch = ProtocolTypeIO.ReadByteArray(ref reader);
        SkinImageWidth = reader.ReadUInt32(true);
        SkinImageHeight = reader.ReadUInt32(true);
        SkinData = ProtocolTypeIO.ReadByteArray(ref reader);
        Animations = ProtocolTypeIO.ReadList(ref reader, static (ref BinaryReader r) =>
        {
            SkinAnimation value = new();
            value.Deserialize(ref r);
            return value;
        });
        CapeImageWidth = reader.ReadUInt32(true);
        CapeImageHeight = reader.ReadUInt32(true);
        CapeData = ProtocolTypeIO.ReadByteArray(ref reader);
        SkinGeometry = ProtocolTypeIO.ReadByteArray(ref reader);
        GeometryDataEngineVersion = ProtocolTypeIO.ReadByteArray(ref reader);
        AnimationData = ProtocolTypeIO.ReadByteArray(ref reader);
        CapeID = reader.ReadVarString();
        FullID = reader.ReadVarString();
        ArmSize = reader.ReadVarString();
        SkinColour = reader.ReadVarString();
        PersonaPieces = ProtocolTypeIO.ReadList(ref reader, static (ref BinaryReader r) =>
        {
            PersonaPiece value = new();
            value.Deserialize(ref r);
            return value;
        });
        PieceTintColours = ProtocolTypeIO.ReadList(ref reader, static (ref BinaryReader r) =>
        {
            PersonaPieceTintColour value = new();
            value.Deserialize(ref r);
            return value;
        });

        Validate();

        PremiumSkin = reader.ReadBool();
        PersonaSkin = reader.ReadBool();
        PersonaCapeOnClassicSkin = reader.ReadBool();
        PrimaryUser = reader.ReadBool();
        OverrideAppearance = reader.ReadBool();
    }

    public void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarString(SkinID);
        writer.WriteVarString(PlayFabID);
        ProtocolTypeIO.WriteByteArray(ref writer, SkinResourcePatch);
        writer.WriteUInt32(SkinImageWidth, true);
        writer.WriteUInt32(SkinImageHeight, true);
        ProtocolTypeIO.WriteByteArray(ref writer, SkinData);
        ProtocolTypeIO.WriteList(ref writer, Animations, static (ref BinaryWriter w, SkinAnimation value) => value.Serialize(ref w));
        writer.WriteUInt32(CapeImageWidth, true);
        writer.WriteUInt32(CapeImageHeight, true);
        ProtocolTypeIO.WriteByteArray(ref writer, CapeData);
        ProtocolTypeIO.WriteByteArray(ref writer, SkinGeometry);
        ProtocolTypeIO.WriteByteArray(ref writer, GeometryDataEngineVersion);
        ProtocolTypeIO.WriteByteArray(ref writer, AnimationData);
        writer.WriteVarString(CapeID);
        writer.WriteVarString(FullID);
        writer.WriteVarString(ArmSize);
        writer.WriteVarString(SkinColour);
        ProtocolTypeIO.WriteList(ref writer, PersonaPieces, static (ref BinaryWriter w, PersonaPiece value) => value.Serialize(ref w));
        ProtocolTypeIO.WriteList(ref writer, PieceTintColours, static (ref BinaryWriter w, PersonaPieceTintColour value) => value.Serialize(ref w));

        Validate();

        writer.WriteBool(PremiumSkin);
        writer.WriteBool(PersonaSkin);
        writer.WriteBool(PersonaCapeOnClassicSkin);
        writer.WriteBool(PrimaryUser);
        writer.WriteBool(OverrideAppearance);
    }

    public void Validate()
    {
        if (SkinImageHeight * SkinImageWidth * 4 != SkinData.Length)
        {
            throw new FormatException($"Expected skin data to be {SkinImageWidth}x{SkinImageHeight} ({SkinImageHeight * SkinImageWidth * 4} bytes), got {SkinData.Length} bytes.");
        }

        if (CapeImageHeight * CapeImageWidth * 4 != CapeData.Length)
        {
            throw new FormatException($"Expected cape data to be {CapeImageWidth}x{CapeImageHeight} ({CapeImageHeight * CapeImageWidth * 4} bytes), got {CapeData.Length} bytes.");
        }

        for (int i = 0; i < Animations.Count; i++)
        {
            SkinAnimation animation = Animations[i];
            if (animation.ImageHeight * animation.ImageWidth * 4 != animation.ImageData.Length)
            {
                throw new FormatException($"Expected animation {i} data to be {animation.ImageWidth}x{animation.ImageHeight} ({animation.ImageHeight * animation.ImageWidth * 4} bytes), got {animation.ImageData.Length} bytes.");
            }
        }
    }
}

public sealed class SkinAnimation
{
    public uint ImageWidth { get; set; }
    public uint ImageHeight { get; set; }
    public byte[] ImageData { get; set; } = [];
    public uint AnimationType { get; set; }
    public float FrameCount { get; set; }
    public uint ExpressionType { get; set; }

    public void Deserialize(ref BinaryReader reader)
    {
        ImageWidth = reader.ReadUInt32(true);
        ImageHeight = reader.ReadUInt32(true);
        ImageData = ProtocolTypeIO.ReadByteArray(ref reader);
        AnimationType = reader.ReadUInt32(true);
        FrameCount = reader.ReadF32(true);
        ExpressionType = reader.ReadUInt32(true);
    }

    public void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt32(ImageWidth, true);
        writer.WriteUInt32(ImageHeight, true);
        ProtocolTypeIO.WriteByteArray(ref writer, ImageData);
        writer.WriteUInt32(AnimationType, true);
        writer.WriteF32(FrameCount, true);
        writer.WriteUInt32(ExpressionType, true);
    }
}

public sealed class PersonaPiece
{
    public string PieceID { get; set; } = string.Empty;
    public string PieceType { get; set; } = string.Empty;
    public string PackID { get; set; } = string.Empty;
    public bool Default { get; set; }
    public string ProductID { get; set; } = string.Empty;

    public void Deserialize(ref BinaryReader reader)
    {
        PieceID = reader.ReadVarString();
        PieceType = reader.ReadVarString();
        PackID = reader.ReadVarString();
        Default = reader.ReadBool();
        ProductID = reader.ReadVarString();
    }

    public void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarString(PieceID);
        writer.WriteVarString(PieceType);
        writer.WriteVarString(PackID);
        writer.WriteBool(Default);
        writer.WriteVarString(ProductID);
    }
}

public sealed class PersonaPieceTintColour
{
    public string PieceType { get; set; } = string.Empty;
    public List<string> Colours { get; set; } = [];

    public void Deserialize(ref BinaryReader reader)
    {
        PieceType = reader.ReadVarString();
        Colours = ProtocolTypeIO.ReadList(ref reader, static (ref BinaryReader r) => r.ReadVarString());
    }

    public void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarString(PieceType);
        ProtocolTypeIO.WriteList(ref writer, Colours, static (ref BinaryWriter w, string value) => w.WriteVarString(value));
    }
}

file static class ProtocolTypeIO
{
    public static byte[] ReadByteArray(ref BinaryReader reader)
    {
        int length = checked((int)reader.ReadVarUInt());
        return reader.ReadBytes(length).ToArray();
    }

    public static void WriteByteArray(ref BinaryWriter writer, ReadOnlySpan<byte> value)
    {
        writer.WriteVarUInt((uint)value.Length);
        writer.WriteBytes(value);
    }

    public static List<T> ReadList<T>(ref BinaryReader reader, ReadItem<T> read)
    {
        int length = checked((int)reader.ReadUInt32(true));
        List<T> list = new(length);
        for (int i = 0; i < length; i++)
        {
            list.Add(read(ref reader));
        }

        return list;
    }

    public static void WriteList<T>(ref BinaryWriter writer, IReadOnlyList<T> values, WriteItem<T> write)
    {
        writer.WriteUInt32((uint)values.Count, true);
        for (int i = 0; i < values.Count; i++)
        {
            write(ref writer, values[i]);
        }
    }

    public delegate T ReadItem<out T>(ref BinaryReader reader);
    public delegate void WriteItem<T>(ref BinaryWriter writer, T value);
}
