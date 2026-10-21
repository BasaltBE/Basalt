using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

public abstract class EntityTrait(Entity entity) {
    public readonly Entity Entity = entity;

    public virtual void OnAdd() { }
    public virtual void OnRemove() { }
    public virtual void OnSpawn() { }
    public virtual void OnRespawn() { }
    public virtual void OnDespawn() { }
    public virtual void OnDeath() { }
    public virtual void OnHurt() { }
    public virtual void OnHurt(EntityHurtEvent hurt) => OnHurt();
    public virtual void OnDeath(EntityDeathEvent death) => OnDeath();
    public virtual void OnMove() { }
    public virtual void OnAttack() { }
    public virtual void OnAttack(Entity target, ItemStack? item) => OnAttack();
    public virtual void OnUseItem(ItemStack? item) { }
    public virtual void OnUseItemOnBlock(ItemStack? item, BlockPermutation block, BlockPos position, byte face, Vec3 clickPosition) { }
    public virtual void OnUseItemOnEntity(ItemStack? item, Entity target, Vec3 hitPosition) { }
    public virtual void Tick() { }
    public virtual void Read(CompoundTag entityTag) { }
    public virtual void Write(CompoundTag entityTag) { }
}
