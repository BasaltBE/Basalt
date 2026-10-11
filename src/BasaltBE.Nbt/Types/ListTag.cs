using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class ListTag : Tag {
    public TagType ElementType;
    public List<Tag> Value = [];
    public override TagType Type => TagType.List;

    internal override void Read(ref BinaryReader reader, Options options) {
        ElementType = Nbt.ReadTagType(ref reader);
        int count = Nbt.ReadLength(ref reader, options);
        if (ElementType == TagType.End && count != 0)
            throw new InvalidDataException("NBT list with End element type must be empty.");

        Value = new List<Tag>(count);
        for (int i = 0; i < count; i++)
            Value.Add(Nbt.Read(ref reader, ElementType, options));
    }

    internal override void Write(ref BinaryWriter writer, Options options) {
        TagType elementType = Value.Count == 0 ? ElementType : Value[0].Type;
        if (elementType == TagType.End && Value.Count != 0)
            throw new InvalidDataException("NBT list with End element type must be empty.");

        foreach (Tag tag in Value)
            if (tag.Type != elementType)
                throw new InvalidDataException("NBT list items must have the same tag type.");

        writer.WriteUInt8((byte)elementType);
        Nbt.WriteLength(ref writer, Value.Count, options);
        foreach (Tag tag in Value)
            Nbt.Write(ref writer, tag, options);
    }
}
