namespace Basalt.Core.Tasks;

using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Enums;
using Basalt.Core.Player;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Enums;
using BedrockProtocol.Types;

internal sealed class LavaCheckTask : ServerTask {
    private readonly Entity _entity;
    private readonly Vec3 _position;
    private readonly Dimension _dimension;
    private readonly int _minX;
    private readonly int _maxX;
    private readonly int _minY;
    private readonly int _maxY;
    private readonly int _minZ;
    private readonly int _maxZ;
    private bool _inLava;
    private bool _inWater;

    public LavaCheckTask(
        Entity entity,
        Vec3 position,
        Dimension dimension,
        int minX,
        int maxX,
        int minY,
        int maxY,
        int minZ,
        int maxZ
    ) {
        _entity = entity;
        _position = position;
        _dimension = dimension;
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;
        _minZ = minZ;
        _maxZ = maxZ;
    }

    public override void Execute() {
        string lava = BlockIdentifier.Lava.ToIdentifier();
        string flowingLava = BlockIdentifier.FlowingLava.ToIdentifier();
        string water = BlockIdentifier.Water.ToIdentifier();
        string flowingWater = BlockIdentifier.FlowingWater.ToIdentifier();
        for (int x = _minX; x <= _maxX; x++) {
            for (int y = _minY; y <= _maxY; y++) {
                for (int z = _minZ; z <= _maxZ; z++) {
                    if (!_dimension.TryGetLoadedPermutation(x, y, z, out BlockPermutation? permutation) ||
                        permutation is null) {
                        continue;
                    }

                    if (string.Equals(permutation.Type.Identifier, lava, StringComparison.Ordinal) ||
                        string.Equals(permutation.Type.Identifier, flowingLava, StringComparison.Ordinal)) {
                        _inLava = true;
                    }
                    else if (string.Equals(permutation.Type.Identifier, water, StringComparison.Ordinal) ||
                             string.Equals(permutation.Type.Identifier, flowingWater, StringComparison.Ordinal)) {
                        _inWater = true;
                    }
                }
            }
        }
    }

    public override void Complete() {
        if (!_entity.IsAlive || _entity.PendingDespawn ||
            MathF.Abs(_entity.Position.X - _position.X) > 0.25f ||
            MathF.Abs(_entity.Position.Y - _position.Y) > 0.25f ||
            MathF.Abs(_entity.Position.Z - _position.Z) > 0.25f) {
            return;
        }

        _entity.IsInWater = _inWater;
        if (_inWater) {
            _entity.SetOnFire(0);
        }

        if (!_inLava) {
            return;
        }

        if (_entity is ItemEntity item) {
            if (!EntityLavaTrait.IsLavaProof(item)) {
                item.Despawn(new EntityDespawnOptions());
            }

            return;
        }

        if (_entity is Player player &&
            player.GetGamemode() is GameType.Survival or GameType.Adventure) {
            _entity.SetOnFire(8 * 20);

            if (_entity.HasEffect(EffectType.FireResistance)) {
                return;
            }

            _entity.GetTrait<EntityHealthTrait>()?.ApplyDamage(
                EntityLavaTrait.Damage,
                null,
                ActorDamageCause.Lava
            );
        }
    }
}
