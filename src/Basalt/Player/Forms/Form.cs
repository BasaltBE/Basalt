namespace Basalt.Core.Forms;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Basalt.Core.Player;
using Basalt.BedrockProtocol.Packets;

public abstract class Form<TResponse> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static uint NextFormId;

    internal readonly uint FormId = Interlocked.Increment(ref NextFormId);
    public string Title;

    protected Form(string title) {
        Title = title;
    }

    [RequiresUnreferencedCode("...")]
    [RequiresDynamicCode("...")]
    public void Show(Player player, Action<TResponse> result) {
        player.PendingForms[FormId] = new PendingForm((data, canceled) => {
            result(canceled ? default! : ReadResponse(data));
        });

        player.Send(new ModalFormRequestPacket {
            FormId = checked((uint)FormId),
            FormUiJson = ToJson()
        });
    }

    public void Close(Player player) {
        player.Send(new ClientboundCloseFormPacket());
    }

    [RequiresUnreferencedCode("...")]
    [RequiresDynamicCode("...")]
    public string ToJson() {
        return JsonSerializer.Serialize(CreatePayload(), JsonOptions);
    }

    protected abstract object CreatePayload();
    protected abstract TResponse ReadResponse(string? data);
}
