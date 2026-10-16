using System.Text.Json;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class RawTextEnum() : CommandEnum(CommandParameterType.RawText)
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
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("rawtext", out JsonElement entries) || entries.ValueKind != JsonValueKind.Array)
                return false;
        }
        catch (JsonException)
        {
            return false;
        }
        value = text;
        return true;
    }
}
