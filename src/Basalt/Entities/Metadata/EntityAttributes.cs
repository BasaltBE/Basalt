namespace Basalt.Core.Entities.Metadata;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Player.Traits;
using Basalt.Core.Worlds;
using System.Text.Json;

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
        SetAttribute(new AttributeData {
            Name = "minecraft:attack_damage",
            Minimum = 0f,
            Maximum = 2048f,
            Current = 2f,
            DefaultMinimum = 0f,
            DefaultMaximum = 2048f,
            Default = 2f,
            Modifiers = []
        });
        SetAttribute(new AttributeData {
            Name = "minecraft:follow_range",
            Minimum = 0f,
            Maximum = 2048f,
            Current = 32f,
            DefaultMinimum = 0f,
            DefaultMaximum = 2048f,
            Default = 32f,
            Modifiers = []
        });
        if (_entity.Type.TryGetComponentProperties("minecraft:follow_range", out JsonElement followRange) &&
            followRange.TryGetProperty("value", out JsonElement value) &&
            value.TryGetSingle(out float range)) {
            AttributeData attribute = _attributes[AttributeName.FollowRange];
            attribute.Current = Math.Clamp(range, attribute.Minimum, attribute.Maximum);
            attribute.Default = attribute.Current;
        }
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

    public bool SetModifier(AttributeName name, string id, float amount, int operation) {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfNegative(operation);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(operation, 2);
        if (!_attributes.TryGetValue(name, out AttributeData? attribute)) {
            return false;
        }

        List<AttributeModifier> modifiers = [.. attribute.Modifiers];
        int index = modifiers.FindIndex(modifier => modifier.Id == id);
        AttributeModifier modifier = new() {
            Id = id,
            Name = id,
            Amount = amount,
            Operation = operation,
            Operand = 0,
            Serializable = true
        };
        if (index >= 0) {
            modifiers[index] = modifier;
        }
        else {
            modifiers.Add(modifier);
        }

        attribute.Modifiers = [.. modifiers];
        Recalculate(attribute);
        _entity.AttributesDirty = true;
        return true;
    }

    public bool RemoveModifier(AttributeName name, string id) {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_attributes.TryGetValue(name, out AttributeData? attribute)) {
            return false;
        }

        int count = attribute.Modifiers.Length;
        attribute.Modifiers = [.. attribute.Modifiers.Where(modifier => modifier.Id != id)];
        if (attribute.Modifiers.Length == count) {
            return false;
        }

        Recalculate(attribute);
        _entity.AttributesDirty = true;
        return true;
    }

    public bool RemoveAttribute(AttributeName name) {
        return _attributes.Remove(name);
    }

    private static void Recalculate(AttributeData attribute) {
        float baseValue = attribute.Default;
        float value = baseValue;
        for (int operation = 0; operation <= 2; operation++) {
            for (int i = 0; i < attribute.Modifiers.Length; i++) {
                AttributeModifier modifier = attribute.Modifiers[i];
                if (modifier.Operation != operation) {
                    continue;
                }

                value = operation switch {
                    0 => value + modifier.Amount,
                    1 => value + baseValue * modifier.Amount,
                    2 => value * (1f + modifier.Amount),
                    _ => value
                };
            }
        }

        attribute.Current = Math.Clamp(value, attribute.Minimum, attribute.Maximum);
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


