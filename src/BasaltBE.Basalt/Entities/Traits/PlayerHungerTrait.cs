using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerHungerTrait(Entity entity) : EntityTrait(entity) {
    private const string HungerAttribute = "minecraft:player.hunger";
    private const string SaturationAttribute = "minecraft:player.saturation";
    private const string ExhaustionAttribute = "minecraft:player.exhaustion";
    private const float Maximum = 20;
    private const float ExhaustionPerBlockSprinted = 0.1f;
    private const float ExhaustionPerBlockSwum = 0.01f;
    private const float ExhaustionPerJump = 0.05f;
    private const float ExhaustionForSprintJump = 0.2f;
    private const float ExhaustionPerDamage = 0.1f;
    private const float ExhaustionThreshold = 4;
    private const float NaturalHealingExhaustion = 6;
    private const ulong EffectInterval = 80;

    private float saturation;
    private float exhaustion;
    private float previousX;
    private float previousY;
    private float previousZ;
    private bool jumping;
    private int pendingFoodSlot = -1;
    private int pendingFoodNetId;
    private ulong pendingFoodFinishAge;
    private ulong pendingFoodSoundAge;

    public float Hunger => Entity.Attributes.Get(HungerAttribute)?.Current ?? Maximum;
    public float Saturation => saturation;
    public float Exhaustion => exhaustion;

    public override void OnAdd() {
        Entity.Attributes.Set(HungerAttribute, 0, Maximum, Maximum, 0, Maximum, Maximum);
        Entity.Attributes.Set(SaturationAttribute, 0, Maximum, 0, 0, Maximum, 0);
        Entity.Attributes.Set(ExhaustionAttribute, 0, ExhaustionThreshold, 0, 0, ExhaustionThreshold, 0);
    }

    public override void OnSpawn() {
        Reset();
        RememberPosition();
    }

    public override void OnRespawn() {
        Reset();
        RememberPosition();
    }

    public override void OnMove() {
        if (Entity is not Player player || player.Dead)
            return;

        float deltaX = Entity.Position.X - previousX;
        float deltaY = Entity.Position.Y - previousY;
        float deltaZ = Entity.Position.Z - previousZ;
        float horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        if (player.GameType is GameType.Survival or GameType.Adventure) {
            if (Entity.Flags.Get(ActorFlag.Swimming))
                AddExhaustion(MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ) * ExhaustionPerBlockSwum);
            else if (Entity.Flags.Get(ActorFlag.Sprinting))
                AddExhaustion(horizontalDistance * ExhaustionPerBlockSprinted);

            if (Entity.GetTrait<EntityMovementTrait>() is { Jumping: true } && !jumping)
                AddExhaustion(ExhaustionPerJump + (Entity.Flags.Get(ActorFlag.Sprinting) ? ExhaustionForSprintJump : 0));
        }

        jumping = Entity.GetTrait<EntityMovementTrait>()?.Jumping ?? false;
        RememberPosition();
    }

    public override void Tick() {
        if (Entity is not Player player || !player.Spawned || player.Disposed || player.Dead)
            return;

        TickEating(player);
        if (player.GameType is not (GameType.Survival or GameType.Adventure))
            return;

        bool peaceful = player.Dimension.Difficulty == Difficulty.Peaceful;
        if (!peaceful) {
            while (exhaustion >= ExhaustionThreshold) {
                exhaustion -= ExhaustionThreshold;
                if (saturation > 0)
                    saturation = Math.Max(0, saturation - 1);
                else
                    Entity.Attributes.SetCurrent(HungerAttribute, Math.Max(0, Hunger - 1));
            }
        }
        Entity.Attributes.SetCurrent(ExhaustionAttribute, exhaustion);
        Entity.Attributes.SetCurrent(SaturationAttribute, saturation);

        if (Entity.SimulationAge % (peaceful ? 20 : EffectInterval) != 0)
            return;

        if (peaceful) {
            Entity.GetTrait<EntityHealthTrait>()?.Heal(1);
            return;
        }

        if (Hunger >= 18 && Entity.GetTrait<EntityHealthTrait>() is { } health && health.Current < health.Maximum) {
            health.Heal(1);
            AddExhaustion(NaturalHealingExhaustion);
        }
        else if (Hunger <= 0 && Entity.GetTrait<EntityHealthTrait>() is { } starvingHealth) {
            float minimum = player.Dimension.Difficulty switch {
                Difficulty.Easy => 10,
                Difficulty.Normal => 1,
                Difficulty.Hard => 0,
                _ => starvingHealth.Current
            };
            if (starvingHealth.Current > minimum)
                starvingHealth.ApplyDamage(Math.Min(1, starvingHealth.Current - minimum), EntityDamageCause.Starvation);
        }
    }

    public bool Eat(ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        if (Entity is not Player player || player.Dead || item.Type.FoodNutrition <= 0 ||
            player.GetTrait<PlayerInventoryTrait>() is not { } inventory ||
            !ReferenceEquals(inventory.Inventory.GetItem(inventory.SelectedSlot), item) ||
            Hunger >= Maximum && !item.Type.FoodCanAlwaysEat)
            return false;

        PlayerEatEvent eat = new(player, item, item.Type.FoodNutrition,
            item.Type.FoodNutrition * item.Type.FoodSaturationModifier * 2);
        if (player.Network.Server.Events.HasSubscribers<PlayerEatEvent>())
            player.Network.Server.Events.Emit(eat);
        if (eat.IsCanceled() || player.Dead ||
            !ReferenceEquals(inventory.Inventory.GetItem(inventory.SelectedSlot), item))
            return false;

        ItemStack? consumed = inventory.Inventory.TakeItem(inventory.SelectedSlot);
        if (consumed is null)
            return false;

        Entity.Attributes.SetCurrent(HungerAttribute, Hunger + Math.Max(0, eat.Nutrition));
        saturation = Math.Clamp(saturation + Math.Max(0, eat.Saturation), 0, Hunger);
        Entity.Attributes.SetCurrent(SaturationAttribute, saturation);
        return true;
    }

    internal void StartEating(int slot, ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        if (Entity is not Player player || player.Dead || !player.Spawned || player.Disposed ||
            player.GameType is not (GameType.Survival or GameType.Adventure) ||
            item.Type.FoodNutrition <= 0 ||
            player.GetTrait<PlayerInventoryTrait>() is not { } inventory ||
            (uint)slot >= inventory.Inventory.ContainerSize ||
            inventory.SelectedSlot != slot ||
            !ReferenceEquals(inventory.Inventory.GetItem(slot), item) ||
            Hunger >= Maximum && !item.Type.FoodCanAlwaysEat) {
            CancelEating();
            return;
        }

        if (pendingFoodSlot == slot && pendingFoodNetId == item.NetId)
            return;

        pendingFoodSlot = slot;
        pendingFoodNetId = item.NetId;
        pendingFoodFinishAge = Entity.SimulationAge + (ulong)item.Type.FoodUseDurationTicks;
        pendingFoodSoundAge = Entity.SimulationAge + 4;
        player.SetFlag(ActorFlag.UsingItem, true);
    }

    internal void CancelEating() {
        pendingFoodSlot = -1;
        pendingFoodNetId = 0;
        pendingFoodFinishAge = 0;
        pendingFoodSoundAge = 0;
        if (Entity is Player player)
            player.SetFlag(ActorFlag.UsingItem, false);
    }

    internal void AddExhaustion(float amount) {
        if (Entity is not Player player || player.GameType is not (GameType.Survival or GameType.Adventure) ||
            !float.IsFinite(amount) || amount <= 0)
            return;

        exhaustion = Math.Min(40, exhaustion + amount);
        Entity.Attributes.SetCurrent(ExhaustionAttribute, Math.Min(exhaustion, ExhaustionThreshold));
    }

    internal void Reset() {
        CancelEating();
        saturation = 0;
        exhaustion = 0;
        Entity.Attributes.SetCurrent(HungerAttribute, Maximum);
        Entity.Attributes.SetCurrent(SaturationAttribute, 0);
        Entity.Attributes.SetCurrent(ExhaustionAttribute, 0);
    }

    private void RememberPosition() {
        previousX = Entity.Position.X;
        previousY = Entity.Position.Y;
        previousZ = Entity.Position.Z;
    }

    private void TickEating(Player player) {
        if (pendingFoodSlot < 0)
            return;

        PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
        ItemStack? item = inventory?.Inventory.GetItem(pendingFoodSlot);
        if (inventory is null || inventory.SelectedSlot != pendingFoodSlot || item is null ||
            item.NetId != pendingFoodNetId || item.Type.FoodNutrition <= 0) {
            CancelEating();
            return;
        }

        if (Entity.SimulationAge >= pendingFoodSoundAge && Entity.SimulationAge < pendingFoodFinishAge) {
            player.SpawnedDimension?.Broadcast(new LevelSoundEventPacket {
                SoundEvent = "eat",
                Position = player.Position,
                ActorIdentifier = player.Type?.Identifier ?? string.Empty,
                ActorUniqueId = unchecked((long)player.RuntimeId)
            }, player.Position);
            pendingFoodSoundAge += 4;
        }

        if (Entity.SimulationAge < pendingFoodFinishAge)
            return;

        CancelEating();
        if (Eat(item)) {
            player.SpawnedDimension?.Broadcast(new LevelSoundEventPacket {
                SoundEvent = "burp",
                Position = player.Position
            }, player.Position);
        }
    }
}
