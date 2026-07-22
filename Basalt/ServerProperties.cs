using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Basalt.Core;

public class ServerProperties {
    protected Dictionary<string, string> StringProperties = [];
    protected Dictionary<string, double> NumericalProperties = [];
    protected Dictionary<string, bool> BooleanProperties = [];
    protected Dictionary<string, List<string>> Comments = [];
    protected List<string> OrderedKeys = [];
    protected HashSet<string> MetadataKeys = [];

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PropertyKeyAttribute(string key) : Attribute {
        public string Key { get; } = key;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PropertyCommentAttribute(params string[] comments) : Attribute {
        public string[] Comments { get; } = comments;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PropertyOrderAttribute(int order) : Attribute {
        public int Order { get; } = order;
    }

    // Seperating LoadFromPath and Load to make it so ppl can load properties files from databases if they want lol
    public static ServerProperties LoadFromPath(string path) {
        ServerProperties props = new();
        if (!File.Exists(path)) {
            return props;
        }

        Load(props, File.ReadAllText(path).AsSpan());
        return props;
    }

    public static void Load(ServerProperties props, ReadOnlySpan<char> raw) {
        // TODO: This  loop is pretty ugly and maybe slow so maybe find a better way in the future
        int start = 0;
        for (int i = 0; i <= raw.Length; i++) {
            if (i != raw.Length && raw[i] != '\n') {
                continue;
            }

            int end = i;
            if (end > start && raw[end - 1] == '\r') {
                end--;
            }

            ReadOnlySpan<char> line = raw[start..end].Trim();
            start = i + 1;
            if (line.Length == 0 || line[0] == '#') {
                continue;
            }

            int indexOf = line.IndexOf('=');
            if (indexOf <= 0) {
                continue;
            }

            string key = new(line[..indexOf].Trim());
            ReadOnlySpan<char> rawValue = line[(indexOf + 1)..].Trim();
            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double nv)) {
                props.NumericalProperties[key] = nv;
                props.BooleanProperties.Remove(key);
                props.StringProperties.Remove(key);
                props.TrackKey(key);
            }
            else if (bool.TryParse(rawValue, out bool bv)) {
                props.BooleanProperties[key] = bv;
                props.NumericalProperties.Remove(key);
                props.StringProperties.Remove(key);
                props.TrackKey(key);
            }
            else {
                props.StringProperties[key] = new string(rawValue);
                props.BooleanProperties.Remove(key);
                props.NumericalProperties.Remove(key);
                props.TrackKey(key);
            }
        }
    }

    public void SaveToPath(string path) {
        File.WriteAllText(path, GetRawText());
    }

    public bool HasProperty(string name) {
        return StringProperties.ContainsKey(name) || NumericalProperties.ContainsKey(name) || BooleanProperties.ContainsKey(name);
    }

    public string? GetStringProperty(string name, string? defaultValue = null) {
        if (StringProperties.TryGetValue(name, out string? value)) {
            return value;
        }

        return defaultValue;
    }
    public double GetNumberProperty(string name, double defaultValue = 0) => NumericalProperties.GetValueOrDefault(name, defaultValue);
    public bool GetBoolProperty(string name, bool defaultValue = false) => BooleanProperties.GetValueOrDefault(name, defaultValue);
    public void SetBoolProperty(string name, bool value) {
        BooleanProperties[name] = value;
        NumericalProperties.Remove(name);
        StringProperties.Remove(name);
        TrackKey(name);
    }

    public void SetStringProperty(string name, string? value) {
        StringProperties[name] = value ?? string.Empty;
        NumericalProperties.Remove(name);
        BooleanProperties.Remove(name);
        TrackKey(name);
    }

    public void SetNumericalProperty(string name, double value) {
        NumericalProperties[name] = value;
        StringProperties.Remove(name);
        BooleanProperties.Remove(name);
        TrackKey(name);
    }

    public void SetComment(string key, string comment) {
        if (!Comments.TryGetValue(key, out List<string>? lines)) {
            lines = [];
            Comments[key] = lines;
        }

        lines.Clear();
        lines.Add(comment);
    }

    public void SetComments(string key, params string[] comments) {
        if (!Comments.TryGetValue(key, out List<string>? lines)) {
            lines = [];
            Comments[key] = lines;
        }

        lines.Clear();
        lines.AddRange(comments);
    }

    public void ApplyMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>() {
        ApplyMetadata(typeof(T));
    }

    public void KeepOnlyMetadata() {
        if (MetadataKeys.Count == 0) {
            return;
        }

        StringProperties = StringProperties
            .Where(kv => MetadataKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        NumericalProperties = NumericalProperties
            .Where(kv => MetadataKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        BooleanProperties = BooleanProperties
            .Where(kv => MetadataKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        Comments = Comments
            .Where(kv => MetadataKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        OrderedKeys = OrderedKeys.Where(MetadataKeys.Contains).ToList();
    }

    public T Parse<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : new() {
        object instance = new T();
        Apply(instance, typeof(T));
        return (T)instance;
    }

    public TInterface Parse<TInterface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TImplementation>() where TImplementation : TInterface, new() {
        object instance = new TImplementation();
        Apply(instance, typeof(TImplementation));
        return (TInterface)instance;
    }

    private void Apply(object instance, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type) {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
            string key = GetMemberKey(field);
            if (TryReadValue(key, field.FieldType, out object? value)) {
                field.SetValue(instance, value);
            }
        }

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (!prop.CanWrite) {
                continue;
            }

            MethodInfo? setMethod = prop.SetMethod;
            if (setMethod is null || !setMethod.IsPublic) {
                continue;
            }

            string key = GetMemberKey(prop);
            if (TryReadValue(key, prop.PropertyType, out object? value)) {
                prop.SetValue(instance, value);
            }
        }
    }

    private static string GetMemberKey(MemberInfo member) {
        PropertyKeyAttribute? custom = member.GetCustomAttribute<PropertyKeyAttribute>();
        if (custom is not null && !string.IsNullOrWhiteSpace(custom.Key)) {
            return custom.Key;
        }

        return ToBdsKey(member.Name);
    }

    private void ApplyMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) {
        object? defaults = Activator.CreateInstance(type);
        List<(string Key, int Order, string[] Comments)> keys = [];
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public)) {
            string key = GetMemberKey(field);
            int order = field.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? int.MaxValue;
            string[] comments = field.GetCustomAttribute<PropertyCommentAttribute>()?.Comments ?? [];
            keys.Add((key, order, comments));
            EnsureDefault(key, field.FieldType, defaults is not null ? field.GetValue(defaults) : null);
        }

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (!prop.CanWrite || prop.SetMethod is null || !prop.SetMethod.IsPublic) {
                continue;
            }

            string key = GetMemberKey(prop);
            int order = prop.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? int.MaxValue;
            string[] comments = prop.GetCustomAttribute<PropertyCommentAttribute>()?.Comments ?? [];
            keys.Add((key, order, comments));
            EnsureDefault(key, prop.PropertyType, defaults is not null ? prop.GetValue(defaults) : null);
        }

        List<string> metadataOrder = keys
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.Key)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        MetadataKeys = metadataOrder.ToHashSet(StringComparer.Ordinal);

        for (int i = 0; i < metadataOrder.Count; i++) {
            string key = metadataOrder[i];
            if (!OrderedKeys.Contains(key)) {
                OrderedKeys.Add(key);
            }
        }
        // .Concat(OrderedKeys.Where(k => !metadataOrder.Contains(k, StringComparer.Ordinal)))
        // .Distinct(StringComparer.Ordinal)
        // .ToList();


        OrderedKeys = metadataOrder
            .Concat(OrderedKeys.Where(k => !metadataOrder.Contains(k, StringComparer.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var item in keys) {
            if (item.Comments.Length > 0) {
                SetComments(item.Key, item.Comments);
            }
        }
    }

    private void EnsureDefault(string key, Type type, object? defaultValue) {
        if (HasProperty(key) || defaultValue is null)
            return;

        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(bool)) {
            BooleanProperties[key] = (bool)defaultValue;
        }
        else if (targetType == typeof(string)) {
            StringProperties[key] = (string)defaultValue;
        }
        else if (targetType.IsPrimitive || targetType == typeof(decimal)) {
            NumericalProperties[key] = Convert.ToDouble(defaultValue, CultureInfo.InvariantCulture);
        }
        else if (targetType.IsEnum) {
            StringProperties[key] = defaultValue.ToString()?.ToLowerInvariant() ?? string.Empty;
        }
    }

    private bool TryReadValue(string key, Type type, out object? value) {
        // TODO: Simplify a lot of these once i get an idea of how to more properly handle all of this
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(string)) {
            value = GetStringProperty(key);
            return value is not null;
        }

        if (targetType == typeof(bool)) {
            if (!BooleanProperties.TryGetValue(key, out bool bv)) {
                value = null;
                return false;
            }

            value = bv;
            return true;
        }

        if (targetType.IsEnum) {
            if (StringProperties.TryGetValue(key, out string? sv) && Enum.TryParse(targetType, sv, true, out object? enumValue)) {
                value = enumValue;
                return true;
            }

            if (NumericalProperties.TryGetValue(key, out double nv)) {
                value = Enum.ToObject(targetType, Convert.ToInt32(nv, CultureInfo.InvariantCulture));
                return true;
            }

            value = null;
            return false;
        }

        if (targetType == typeof(int)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToInt32(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(uint)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToUInt32(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(short)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToInt16(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(ushort)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToUInt16(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(long)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToInt64(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(ulong)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToUInt64(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(float)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToSingle(nv, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(double)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = nv;
            return true;
        }

        if (targetType == typeof(decimal)) {
            if (!NumericalProperties.TryGetValue(key, out double nv)) {
                value = null;
                return false;
            }

            value = Convert.ToDecimal(nv, CultureInfo.InvariantCulture);
            return true;
        }

        value = null;
        return false;
    }

    // Con wanted to use BDS key names but they're not always valid cuz of - so all this is just to satisfy the parser
    private static string ToBdsKey(string name) {
        if (string.IsNullOrEmpty(name)) {
            return name;
        }

        StringBuilder sb = new(name.Length + 8);
        for (int i = 0; i < name.Length; i++) {
            char c = name[i];
            if (c == '_') {
                sb.Append('-');
                continue;
            }

            if (char.IsUpper(c)) {
                if (i > 0) {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    public string GetRawText() {
        StringBuilder sb = new();
        foreach (string key in OrderedKeys) {
            if (Comments.TryGetValue(key, out List<string>? comments)) {
                for (int i = 0; i < comments.Count; i++) {
                    sb.Append("# ");
                    sb.Append(comments[i]);
                    sb.Append('\n');
                }
            }

            sb.Append(key);
            sb.Append('=');
            if (BooleanProperties.TryGetValue(key, out bool bv)) {
                sb.Append(bv.ToString().ToLowerInvariant());
            }
            else if (NumericalProperties.TryGetValue(key, out double nv)) {
                sb.Append(nv.ToString(CultureInfo.InvariantCulture));
            }
            else if (StringProperties.TryGetValue(key, out string? sv)) {
                sb.Append(sv);
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private void TrackKey(string key) {
        if (!OrderedKeys.Contains(key)) {
            OrderedKeys.Add(key);
        }
    }
}






