namespace Basalt.BedrockProtocol.Enums;

public enum ScorePacketEntryAction : byte {
    Remove = 0,
    ChangePlayer = 1,
    ChangeEntity = 2,
    ChangeFakePlayer = 3
}
