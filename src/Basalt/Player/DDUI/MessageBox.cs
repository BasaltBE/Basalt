namespace Basalt.Core.DDUI;

public sealed class MessageBox : DataDrivenScreen {
    public override string Identifier => "minecraft:message_box";

    Action<Player.Player, int>? _closeHandler;
    int _selection;

    public MessageBox(string title, string body = "") {
        Set(DduiProperty.String("title", title));
        Set(DduiProperty.String("body", body));
    }

    public MessageBox Button1(string label, string? tooltip = null) {
        DduiProperty btn = DduiProperty.Object("button1");
        btn.Set(DduiProperty.String("label", label));
        btn.Set(DduiProperty.Boolean("button_visible", true));
        btn.Set(DduiProperty.Boolean("visible", true));
        DduiProperty onClick = btn.Set(DduiProperty.Long("onClick", 0));

        if (tooltip is not null) {
            btn.Set(DduiProperty.String("tooltip", tooltip));
            btn.Set(DduiProperty.Boolean("tooltip_visible", true));
        }

        onClick.Listen((player, _) => {
            _selection = 1;
            Dismiss(player);
            _closeHandler?.Invoke(player, _selection);
            return false;
        });

        Set(btn);
        return this;
    }

    public MessageBox Button2(string label, string? tooltip = null) {
        DduiProperty btn = DduiProperty.Object("button2");
        btn.Set(DduiProperty.String("label", label));
        btn.Set(DduiProperty.Boolean("button_visible", true));
        btn.Set(DduiProperty.Boolean("visible", true));
        DduiProperty onClick = btn.Set(DduiProperty.Long("onClick", 0));

        if (tooltip is not null) {
            btn.Set(DduiProperty.String("tooltip", tooltip));
            btn.Set(DduiProperty.Boolean("tooltip_visible", true));
        }

        onClick.Listen((player, _) => {
            _selection = 2;
            Dismiss(player);
            _closeHandler?.Invoke(player, _selection);
            return false;
        });

        Set(btn);
        return this;
    }

    public MessageBox WhenClosed(Action<Player.Player, int> handler) {
        _closeHandler = handler;
        return this;
    }
}
