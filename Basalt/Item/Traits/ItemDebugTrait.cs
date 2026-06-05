namespace Basalt.Core.Item.Traits;

using Basalt.Core.Item.Traits.Types;
using Player = Basalt.Core.Player.Player;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;


public sealed class ItemDebugTrait : ItemTrait
{
    public new static string Identifier => "item_debug";

    public ItemDebugTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public override void OnUseOnAir(ItemUseOnAirDetails details)
    {
        Send(details.Player, "OnUseOnAir");
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details)
    {
        Send(details.Player, "OnUseOnBlock");
    }

    public override void OnPlace(ItemPlaceDetails details)
    {
        Send(details.Player, "OnPlace");
    }

    public override void OnUseOnEntity(ItemUseOnEntityDetails details)
    {
        Send(details.Player, $"OnUseOnEntity target={details.Target.Identifier}");
    }

    public override void OnUseAttack(ItemUseAttackDetails details)
    {
        Send(details.Player, $"OnUseAttack target={details.Target.Identifier}");
    }

    private static void Send(Player player, string message)
    {
        player.Send(new TextPacket
        {
            NeedsTranslation = false,
            VariantType = TextVariantType.MessageOnly,
            Variant = new TextVariant
            {
                Type = TextType.Tip,
                Message = $"ItemDebugTrait: {message}"
            },
            Xuid = string.Empty,
            PlatformChatId = string.Empty,
            FilteredMessage = null
        });
    }
}







