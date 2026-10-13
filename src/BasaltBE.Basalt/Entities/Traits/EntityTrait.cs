using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities.Traits;

public abstract class EntityTrait(Entity entity) {
    public readonly Entity Entity = entity;

    public virtual void OnAdd() { }
    public virtual void OnRemove() { }
    public virtual void OnSpawn() { }
    public virtual void OnDespawn() { }
    public virtual void OnDeath() { }
    public virtual void OnHurt() { }
    public virtual void OnMove() { }
    public virtual void OnAttack() { }
    public virtual void Tick() { }
    public virtual void Read(CompoundTag entityTag) { }
    public virtual void Write(CompoundTag entityTag) { }
}
