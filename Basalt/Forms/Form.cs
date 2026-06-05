namespace Basalt.Core.Forms;

using System.Text.Json;
using Basalt.Core.Player;
using Basalt.Protocol.Packets;

public abstract class Form<TResponse>
{
    private static int NextFormId;

    internal readonly int FormId = Interlocked.Increment(ref NextFormId);
    public string Title;

    protected Form(string title)
    {
        Title = title;
    }

    public void Show(Player player, Action<TResponse> result)
    {
        player.PendingForms[FormId] = new FormParticipant((data, canceled) =>
        {
            result(canceled ? default! : ReadResponse(data));
        });

        player.Send(new ModalFormRequestPacket
        {
            FormId = FormId,
            Payload = ToJson()
        });
    }

    public void Close(Player player)
    {
        player.Send(new ClientboundCloseFormPacket());
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(CreatePayload());
    }

    protected abstract object CreatePayload();
    protected abstract TResponse ReadResponse(string? data);
}
