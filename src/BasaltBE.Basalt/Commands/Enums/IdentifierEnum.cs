using System.Collections.Frozen;

namespace BasaltBE.Basalt.Commands.Enums;

public abstract class IdentifierEnum<T> : CustomEnum where T : class {
    private readonly FrozenDictionary<string, T> values;

    protected IdentifierEnum(string identifier, FrozenDictionary<string, T> values)
        : base(identifier, values.Keys) {
        this.values = values;
    }

    protected static FrozenDictionary<string, T> Index(IEnumerable<T> values, Func<T, string> identifier) {
        Dictionary<string, T> index = new(StringComparer.Ordinal);
        foreach (T value in values) {
            string name = identifier(value);
            index.Add(name, value);
            if (name.StartsWith("minecraft:", StringComparison.Ordinal)) index.TryAdd(name[10..], value);
        }

        return index.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token)) return false;

        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"') token = token[1..^1];
        if (!values.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(token, out T? found)) return false;

        value = found;
        return true;
    }
}
