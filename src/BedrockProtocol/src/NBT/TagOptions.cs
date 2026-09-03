namespace Basalt.BedrockProtocol.NBT;
public readonly record struct TagOptions(
    bool Name = true,
    bool Type = true,
    bool VarInt = false
) {
    public TagOptions() : this(true, true, false) {
    }
}
