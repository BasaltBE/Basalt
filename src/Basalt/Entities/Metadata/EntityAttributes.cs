namespace Basalt.Core.Entities.Metadata;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Player.Traits;
using Basalt.Core.Worlds;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class EntityAttributes {
    private readonly Dictionary<AttributeName, AttributeData> _attributes = [];
    private readonly Entity _entity;

    internal EntityAttributes(Entity entity) {
        _entity = entity;

        SetAttribute(new AttributeData {
            Name = "minecraft:absorption",
            Minimum = 0f,
            Maximum = float.MaxValue,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = float.MaxValue,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.hunger",
            Minimum = 0f,
            Maximum = 20f,
            Current = 20f,
            DefaultMinimum = 0f,
            DefaultMaximum = 20f,
            Default = 20f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:knockback_resistance",
            Minimum = 0f,
            Maximum = 1f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 1f,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:health",
            Minimum = 0f,
            Maximum = 20f,
            Current = 20f,
            DefaultMinimum = 0f,
            DefaultMaximum = 20f,
            Default = 20f,
            Modifiers = []
        });
        RegisterWithCurrent(AttributeName.Movement, 0f, float.MaxValue, 0.1f, 0.1f);
        SetAttribute(new AttributeData {
            Name = "minecraft:player.saturation",
            Minimum = 0f,
            Maximum = 20f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 20f,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.exhaustion",
            Minimum = 0f,
            Maximum = 5f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 5f,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.level",
            Minimum = 0f,
            Maximum = 24791f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 24791f,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.experience",
            Minimum = 0f,
            Maximum = 1f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 1f,
            Default = 0f,
            Modifiers = []
        });
        RegisterWithCurrent(AttributeName.UnderwaterMovement, 0f, float.MaxValue, 0.02f, 0.02f);
        SetAttribute(new AttributeData {
            Name = "minecraft:luck",
            Minimum = -1024f,
            Maximum = 1024f,
            Current = 0f,
            DefaultMinimum = -1024f,
            DefaultMaximum = 1024f,
            Default = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:fall_damage",
            Minimum = 0f,
            Maximum = float.MaxValue,
            Current = 1f,
            DefaultMinimum = 0f,
            DefaultMaximum = float.MaxValue,
            Default = 1f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:horse.jump_strength",
            Minimum = 0f,
            Maximum = 2f,
            Current = 0.7f,
            DefaultMinimum = 0f,
            DefaultMaximum = 2f,
            Default = 0.7f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:zombie.spawn_reinforcements",
            Minimum = 0f,
            Maximum = 1f,
            Current = 0f,
            DefaultMinimum = 0f,
            DefaultMaximum = 1f,
            Default = 0f,
            Modifiers = []
        });
        RegisterWithCurrent(AttributeName.LavaMovement, 0f, float.MaxValue, 0.02f, 0.02f);
    }

    private void RegisterWithCurrent(
        AttributeName name,
        float min,
        float max,
        float current,
        float @default
    ) {
        SetAttribute(new AttributeData {
            Name = name.ToProtocolString(),
            Minimum = min,
            Maximum = max,
            Current = current,
            DefaultMinimum = min,
            DefaultMaximum = max,
            Default = @default,
            Modifiers = []
        });
    }

    public IReadOnlyList<AttributeData> GetAll() {
        return _attributes.Values.ToList();
    }

    public bool HasAttribute(AttributeName name) {
        return _attributes.ContainsKey(name);
    }

    public AttributeData? GetAttribute(AttributeName name) {
        return _attributes.TryGetValue(name, out AttributeData? attribute) ? attribute : null;
    }

    public void SetAttribute(AttributeData attribute) {
        ArgumentNullException.ThrowIfNull(attribute);
        _attributes[AttributeNameExtensions.FromProtocolString(attribute.Name)] = attribute;
    }

    public bool RemoveAttribute(AttributeName name) {
        return _attributes.Remove(name);
    }

    /// <summary>
    /// Sends current attributes to the owning player's client.
    /// No-op if the entity is not a connected player.
    /// </summary>
    public void Send(bool immediate = false) {
        if (_entity is not Player.Player player) {
            return;
        }

        if (player.Network is null || player.Connection is null) {
            return;
        }

        AttributeData? hunger = GetAttribute(AttributeName.PlayerHunger);
        AttributeData? saturation = GetAttribute(AttributeName.PlayerSaturation);
        AttributeData? exhaustion = GetAttribute(AttributeName.PlayerExhaustion);
        PlayerHungerTrait? hungerTrait = player.GetTrait<PlayerHungerTrait>();
        if (hungerTrait is not null) {
            saturation!.Current = Math.Clamp(hungerTrait.Saturation, saturation.Minimum, saturation.Maximum);
            exhaustion!.Current = Math.Clamp(hungerTrait.Exhaustion, exhaustion.Minimum, exhaustion.Maximum);
        }


        List<AttributeData> foodAttributes = [
            hunger!,
            saturation!,
            exhaustion!,
        ];
        ulong tick = player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0UL;
        UpdateAttributesPacket foodPacket = new() {
            ActorRuntimeId = player.RuntimeId,
            Tick = tick,
            Attributes = foodAttributes.ToArray(),
        };
        List<AttributeData> playerAttributes = [
            GetAttribute(AttributeName.Absorption)!,
            GetAttribute(AttributeName.Health)!,
            GetAttribute(AttributeName.PlayerLevel)!,
            GetAttribute(AttributeName.PlayerExperience)!,
            GetAttribute(AttributeName.Movement)!,
        ];
        UpdateAttributesPacket packet = new() {
            ActorRuntimeId = player.RuntimeId,
            Tick = tick,
            Attributes = playerAttributes.ToArray(),
        };

        if (immediate) {
            player.Network.SendPacket(player.Connection, foodPacket);
        }
        else {
            player.Network.QueuePacket(player.Connection, foodPacket);
        }

        if (packet.Attributes.Length > 0) {
            if (immediate) {
                player.Network.SendPacket(player.Connection, packet);
            }
            else {
                player.Network.QueuePacket(player.Connection, packet);
            }
        }

        player.AttributesDirty = false;
    }
}


