namespace Basalt.Core.Blocks.Types;

public readonly struct BlockStateValue
{
    private readonly long _number;
    private readonly string? _text;
    private readonly bool _flag;

    public byte Kind { get; }

    private BlockStateValue(long number)
    {
        Kind = 0;
        _number = number;
        _text = null;
        _flag = default;
    }

    private BlockStateValue(string text)
    {
        Kind = 1;
        _number = default;
        _text = text;
        _flag = default;
    }

    private BlockStateValue(bool flag)
    {
        Kind = 2;
        _number = default;
        _text = null;
        _flag = flag;
    }

    public long AsNumber() => Kind == 0 ? _number : throw new InvalidOperationException("BlockStateValue is not a number.");
    public string AsString() => Kind == 1 ? _text! : throw new InvalidOperationException("BlockStateValue is not a string.");
    public bool AsBool() => Kind == 2 ? _flag : throw new InvalidOperationException("BlockStateValue is not a bool.");

    public static implicit operator BlockStateValue(byte value) => new(value);
    public static implicit operator BlockStateValue(short value) => new(value);
    public static implicit operator BlockStateValue(int value) => new(value);
    public static implicit operator BlockStateValue(long value) => new(value);
    public static implicit operator BlockStateValue(string value) => new(value);
    public static implicit operator BlockStateValue(bool value) => new(value);
}

public sealed class BlockState : Dictionary<string, BlockStateValue>;







