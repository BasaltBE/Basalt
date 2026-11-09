namespace BasaltBE.Protocol.Enums;

public enum SubChunkRequestResult : byte {
    Unknown,
    Success,
    LevelChunkDoesNotExist,
    WrongDimension,
    PlayerDoesNotExist,
    IndexOutOfBounds,
    SuccessAllAir
}
