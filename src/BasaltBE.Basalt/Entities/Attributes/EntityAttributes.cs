using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Attributes;

public sealed class EntityAttributes {
    readonly Dictionary<string, EntityAttribute> values = new(StringComparer.Ordinal);
    HashSet<string>? dirty;

    public EntityAttribute? Get(string name) => values.TryGetValue(name, out EntityAttribute value) ? value : null;

    internal SyncedAttribute[] CreateSnapshot() {
        SyncedAttribute[] attributes = new SyncedAttribute[values.Count];
        int index = 0;
        foreach (EntityAttribute attribute in values.Values) {
            attributes[index++] = new SyncedAttribute {
                Name = attribute.Name,
                Minimum = attribute.Minimum,
                Maximum = attribute.Maximum,
                Current = attribute.Current
            };
        }
        return attributes;
    }

    public bool Set(
        string name,
        float minimum,
        float maximum,
        float current,
        float defaultMinimum,
        float defaultMaximum,
        float @default,
        AttributeModifier[]? modifiers = null
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimum, maximum);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultMinimum, defaultMaximum);
        EntityAttribute attribute = new(
            name,
            minimum,
            maximum,
            Math.Clamp(current, minimum, maximum),
            defaultMinimum,
            defaultMaximum,
            Math.Clamp(@default, defaultMinimum, defaultMaximum),
            modifiers
        );
        if (values.TryGetValue(name, out EntityAttribute existing) && Same(existing, attribute))
            return false;

        values[name] = attribute;
        (dirty ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
        return true;
    }

    public bool SetCurrent(string name, float current) {
        if (!values.TryGetValue(name, out EntityAttribute existing))
            return false;
        float value = Math.Clamp(current, existing.Minimum, existing.Maximum);
        if (existing.Current == value)
            return false;

        values[name] = new EntityAttribute(
            existing.Name,
            existing.Minimum,
            existing.Maximum,
            value,
            existing.DefaultMinimum,
            existing.DefaultMaximum,
            existing.Default,
            existing.Modifiers
        );
        (dirty ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
        return true;
    }

    public bool SetModifier(string name, AttributeModifier modifier) {
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(modifier.Id);
        ArgumentOutOfRangeException.ThrowIfNegative(modifier.Operation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(modifier.Operation, 2);
        if (!values.TryGetValue(name, out EntityAttribute existing))
            return false;

        AttributeModifier[] modifiers = [.. existing.Modifiers];
        int index = Array.FindIndex(modifiers, item => item.Id == modifier.Id);
        if (index >= 0)
            modifiers[index] = modifier;
        else
            Array.Resize(ref modifiers, modifiers.Length + 1);
        if (index < 0)
            modifiers[^1] = modifier;

        float current = Calculate(existing.Default, modifiers, existing.Minimum, existing.Maximum);
        values[name] = new EntityAttribute(
            existing.Name,
            existing.Minimum,
            existing.Maximum,
            current,
            existing.DefaultMinimum,
            existing.DefaultMaximum,
            existing.Default,
            modifiers
        );
        (dirty ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
        return true;
    }

    public bool RemoveModifier(string name, string id) {
        if (!values.TryGetValue(name, out EntityAttribute existing))
            return false;

        int index = Array.FindIndex(existing.Modifiers, modifier => modifier.Id == id);
        if (index < 0)
            return false;

        AttributeModifier[] modifiers = new AttributeModifier[existing.Modifiers.Length - 1];
        Array.Copy(existing.Modifiers, 0, modifiers, 0, index);
        Array.Copy(existing.Modifiers, index + 1, modifiers, index, modifiers.Length - index);
        values[name] = new EntityAttribute(
            existing.Name,
            existing.Minimum,
            existing.Maximum,
            Calculate(existing.Default, modifiers, existing.Minimum, existing.Maximum),
            existing.DefaultMinimum,
            existing.DefaultMaximum,
            existing.Default,
            modifiers
        );
        (dirty ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
        return true;
    }

    public UpdateAttributesPacket? CreateUpdate(ulong runtimeId, bool all = false, ulong tick = 0) {
        if (values.Count == 0 || (!all && dirty is null))
            return null;

        int count = all ? values.Count : dirty!.Count;
        AttributeData[] attributes = new AttributeData[count];
        int index = 0;
        if (all) {
            foreach (EntityAttribute attribute in values.Values)
                attributes[index++] = attribute.ToProtocolData();
        }
        else {
            foreach (string name in dirty!)
                attributes[index++] = values[name].ToProtocolData();
        }

        dirty = null;
        return new UpdateAttributesPacket { ActorRuntimeId = runtimeId, Attributes = attributes, Tick = tick };
    }

    static bool Same(EntityAttribute left, EntityAttribute right) =>
        left.Minimum == right.Minimum &&
        left.Maximum == right.Maximum &&
        left.Current == right.Current &&
        left.DefaultMinimum == right.DefaultMinimum &&
        left.DefaultMaximum == right.DefaultMaximum &&
        left.Default == right.Default &&
        ReferenceEquals(left.Modifiers, right.Modifiers);

    static float Calculate(float @default, AttributeModifier[] modifiers, float minimum, float maximum) {
        float value = @default;
        for (int operation = 0; operation <= 2; operation++) {
            for (int i = 0; i < modifiers.Length; i++) {
                AttributeModifier modifier = modifiers[i];
                if (modifier.Operation != operation)
                    continue;

                value = operation switch {
                    0 => value + modifier.Amount,
                    1 => value + @default * modifier.Amount,
                    2 => value * (1f + modifier.Amount),
                    _ => value
                };
            }
        }
        return Math.Clamp(value, minimum, maximum);
    }
}
