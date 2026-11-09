using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class EquipmentSlotEnum() : CommandEnum(CommandParameterType.EquipmentSlot) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) || token is not
            ("slot.weapon.mainhand" or "slot.weapon.offhand" or "slot.armor.head" or "slot.armor.chest" or
             "slot.armor.legs" or "slot.armor.feet" or "slot.armor.body" or "slot.hotbar" or "slot.inventory" or
             "slot.enderchest" or "slot.saddle" or "slot.armor" or "slot.chest" or "slot.equippable"))
            return false;
        value = token.ToString();
        return true;
    }
}
