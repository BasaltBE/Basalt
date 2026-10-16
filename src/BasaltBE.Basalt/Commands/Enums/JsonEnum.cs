using System.Text.Json;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

/// <summary>Returns validated JSON text. The command owns any subsequent deserialization.</summary>
public sealed class JsonEnum() : CommandEnum(CommandParameterType.Json)
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        string text = token.ToString();
        try
        {
            using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 16 });
        }
        catch (JsonException)
        {
            return false;
        }
        value = text;
        return true;
    }
}
