using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities;

public class Entity {
    private static long nextUniqueId = 0;
    private static long nextRuntimeId = 0;
    EntityTrait[]? traits;

    public Entity(EntityType? type = null) {
        Type = type;
        traits = EntityPalette.CreateTraits(this, type);
        if (traits is not null) {
            foreach (EntityTrait trait in traits)
                trait.OnAdd();
        }
    }

    public long UniqueId = Interlocked.Increment(ref nextUniqueId);
    public ulong RuntimeId = checked((ulong)Interlocked.Increment(ref nextRuntimeId));
    public EntityType? Type;

    public void AddTrait(EntityTrait trait) {
        ArgumentNullException.ThrowIfNull(trait);
        if (!ReferenceEquals(trait.Entity, this))
            throw new ArgumentException("Trait belongs to a different entity.", nameof(trait));
        if (GetTrait(trait.GetType()) is not null)
            throw new InvalidOperationException($"Entity already has trait '{trait.GetType()}'.");

        if (traits is null) {
            traits = [trait];
        }
        else {
            EntityTrait[] expanded = new EntityTrait[traits.Length + 1];
            traits.CopyTo(expanded, 0);
            expanded[^1] = trait;
            traits = expanded;
        }

        trait.OnAdd();
    }

    public bool RemoveTrait<TTrait>() where TTrait : EntityTrait {
        if (traits is null)
            return false;

        for (int i = 0; i < traits.Length; i++) {
            if (traits[i] is not TTrait)
                continue;

            EntityTrait removed = traits[i];
            if (traits.Length == 1) {
                traits = null;
            }
            else {
                EntityTrait[] reduced = new EntityTrait[traits.Length - 1];
                if (i > 0)
                    Array.Copy(traits, 0, reduced, 0, i);
                if (i < reduced.Length)
                    Array.Copy(traits, i + 1, reduced, i, reduced.Length - i);
                traits = reduced;
            }

            removed.OnRemove();
            return true;
        }

        return false;
    }

    public TTrait? GetTrait<TTrait>() where TTrait : EntityTrait {
        if (traits is null)
            return null;

        foreach (EntityTrait trait in traits) {
            if (trait is TTrait match)
                return match;
        }

        return null;
    }

    public void OnSpawn() => Dispatch(static trait => trait.OnSpawn());
    public void OnDespawn() => Dispatch(static trait => trait.OnDespawn());
    public void OnDeath() => Dispatch(static trait => trait.OnDeath());
    public void OnHurt() => Dispatch(static trait => trait.OnHurt());
    public void OnMove() => Dispatch(static trait => trait.OnMove());
    public void OnAttack() => Dispatch(static trait => trait.OnAttack());
    public void Tick() => Dispatch(static trait => trait.Tick());

    public void Read(CompoundTag entityTag) {
        ArgumentNullException.ThrowIfNull(entityTag);
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            trait.Read(entityTag);
    }

    public void Write(CompoundTag entityTag) {
        ArgumentNullException.ThrowIfNull(entityTag);
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            trait.Write(entityTag);
    }

    void Dispatch(Action<EntityTrait> callback) {
        if (traits is null)
            return;

        foreach (EntityTrait trait in traits)
            callback(trait);
    }

    EntityTrait? GetTrait(Type traitType) {
        if (traits is null)
            return null;

        foreach (EntityTrait trait in traits) {
            if (trait.GetType() == traitType)
                return trait;
        }

        return null;
    }
}
