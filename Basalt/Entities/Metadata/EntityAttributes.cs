namespace Basalt.Core.Entities.Metadata;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Worlds;

using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public sealed class EntityAttributes {
    private readonly Dictionary<AttributeName, AttributeData> _attributes = [];
    private readonly Entity _entity;

    internal EntityAttributes(Entity entity) {
        _entity = entity;

        SetAttribute(new AttributeData {
            Name = "minecraft:absorption",
            MinValue = 0f,
            MaxValue = float.MaxValue,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = float.MaxValue,
            DefaultValue = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.hunger",
            MinValue = 0f,
            MaxValue = 20f,
            CurrentValue = 20f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 20f,
            DefaultValue = 20f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:knockback_resistance",
            MinValue = 0f,
            MaxValue = 1f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 1f,
            DefaultValue = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:health",
            MinValue = 0f,
            MaxValue = 20f,
            CurrentValue = 20f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 20f,
            DefaultValue = 20f,
            Modifiers = []
        });
        RegisterWithCurrent(AttributeName.Movement, 0f, float.MaxValue, 0.1f, 0.1f);
        SetAttribute(new AttributeData {
            Name = "minecraft:player.saturation",
            MinValue = 0f,
            MaxValue = 20f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 20f,
            DefaultValue = 20f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.exhaustion",
            MinValue = 0f,
            MaxValue = 5f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 5f,
            DefaultValue = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.level",
            MinValue = 0f,
            MaxValue = 24791f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 24791f,
            DefaultValue = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:player.experience",
            MinValue = 0f,
            MaxValue = 1f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 1f,
            DefaultValue = 0f,
            Modifiers = []
        });
        RegisterWithCurrent(AttributeName.UnderwaterMovement, 0f, float.MaxValue, 0.02f, 0.02f);
        SetAttribute(new AttributeData {
            Name = "minecraft:luck",
            MinValue = -1024f,
            MaxValue = 1024f,
            CurrentValue = 0f,
            DefaultMinValue = -1024f,
            DefaultMaxValue = 1024f,
            DefaultValue = 0f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:fall_damage",
            MinValue = 0f,
            MaxValue = float.MaxValue,
            CurrentValue = 1f,
            DefaultMinValue = 0f,
            DefaultMaxValue = float.MaxValue,
            DefaultValue = 1f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:horse.jump_strength",
            MinValue = 0f,
            MaxValue = 2f,
            CurrentValue = 0.7f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 2f,
            DefaultValue = 0.7f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:zombie.spawn_reinforcements",
            MinValue = 0f,
            MaxValue = 1f,
            CurrentValue = 0f,
            DefaultMinValue = 0f,
            DefaultMaxValue = 1f,
            DefaultValue = 0f,
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
            MinValue = min,
            MaxValue = max,
            CurrentValue = current,
            DefaultMinValue = min,
            DefaultMaxValue = max,
            DefaultValue = @default,
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

        // Logger.Info($"Attributes send: unique={player.UniqueId}, runtime={player.RuntimeId}, hunger={hunger?.CurrentValue}/{hunger?.MaxValue}, saturation={saturation?.CurrentValue}/{saturation?.MaxValue}, exhaustion={exhaustion?.CurrentValue}/{exhaustion?.MaxValue}, tick=0");

        List<AttributeData> foodAttributes = [
            hunger!,
            saturation!,
            exhaustion!,
        ];
        UpdateAttributesPacket foodPacket = new() {
            TargetRuntimeID = new ActorRuntimeID() {
                Value = player.RuntimeId,
            },
            Tick = new PlayerInputTick() {
                InputTick = 0,
            },
            AttributeList = foodAttributes,
        };
        List<AttributeData> playerAttributes = [
            GetAttribute(AttributeName.Absorption)!,
            GetAttribute(AttributeName.Health)!,
            GetAttribute(AttributeName.PlayerLevel)!,
            GetAttribute(AttributeName.PlayerExperience)!,
            GetAttribute(AttributeName.Movement)!,
        ];
        UpdateAttributesPacket packet = new() {
            TargetRuntimeID = new ActorRuntimeID() {
                Value = player.RuntimeId,
            },
            Tick = new PlayerInputTick() {
                InputTick = 0,
            },
            AttributeList = playerAttributes,
        };

        if (immediate) {
            player.Network.SendPacket(player.Connection, foodPacket);
        }
        else {
            player.Network.QueuePacket(player.Connection, foodPacket);
        }

        if (packet.AttributeList.Count > 0) {
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




