using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class FilepathEnum() : CommandEnum(CommandParameterType.Filepath) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token)) return false;

        string path = CommandReader.Text(token);
        if (path.Length == 0 || path.Contains('\0')) return false;

        value = path;
        return true;
    }
}
