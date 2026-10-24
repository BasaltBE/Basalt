using BasaltBE.Basalt.Entities;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerExperienceTrait(Entity entity) : EntityTrait(entity) {
    private const string LevelAttribute = "minecraft:player.level";
    private const string ExperienceAttribute = "minecraft:player.experience";
    public const int MaximumLevel = 24791;

    public int Level;
    public int TotalExperience;
    public float Progress;
    public int Points => (int)(Progress * PointsToNextLevel);
    public int PointsToNextLevel => Level >= 30 ? 112 + (Level - 30) * 9 : Level >= 15 ? 37 + (Level - 15) * 5 : 7 + Level * 2;

    public override void OnAdd() {
        Entity.Attributes.Set(LevelAttribute, 0, MaximumLevel, 0, 0, MaximumLevel, 0);
        Entity.Attributes.Set(ExperienceAttribute, 0, 1, 0, 0, 1, 0);
    }

    public override void Read(CompoundTag entityTag) {
        int level = entityTag.Get<IntTag>("XpLevel")?.Value ?? 0;
        float progress = entityTag.Get<FloatTag>("XpP")?.Value ?? 0;
        Level = Math.Clamp(level, 0, MaximumLevel);
        Progress = float.IsFinite(progress) ? Math.Clamp(progress, 0, MathF.BitDecrement(1)) : 0;
        TotalExperience = Math.Max(0, entityTag.Get<IntTag>("XpTotal")?.Value ?? 0);
        Sync();
    }

    public override void Write(CompoundTag entityTag) {
        entityTag.Add("XpP", new FloatTag { Value = Progress });
        entityTag.Add("XpLevel", new IntTag { Value = Level });
        entityTag.Add("XpTotal", new IntTag { Value = TotalExperience });
    }

    public void AddPoints(int amount) {
        if (amount == 0)
            return;

        TotalExperience = (int)Math.Clamp((long)TotalExperience + amount, 0, int.MaxValue);
        Progress += (float)amount / PointsToNextLevel;
        while (Progress < 0) {
            float remaining = Progress * PointsToNextLevel;
            if (Level > 0) {
                Level--;
                Progress = 1 + remaining / PointsToNextLevel;
            }
            else {
                Progress = 0;
                break;
            }
        }
        while (Progress >= 1 && Level < MaximumLevel) {
            Progress = (Progress - 1) * PointsToNextLevel;
            Level++;
            Progress /= PointsToNextLevel;
        }
        if (Level == MaximumLevel)
            Progress = Math.Clamp(Progress, 0, MathF.BitDecrement(1));
        Sync();
    }

    public void AddLevels(int amount) {
        if (amount == 0)
            return;

        long level = (long)Level + amount;
        if (level < 0) {
            Level = 0;
            Progress = 0;
            TotalExperience = 0;
        }
        else {
            Level = (int)Math.Min(level, MaximumLevel);
        }
        Sync();
    }

    public bool SetPoints(int amount) {
        if ((uint)amount >= PointsToNextLevel)
            return false;
        Progress = (float)amount / PointsToNextLevel;
        Sync();
        return true;
    }

    public void SetLevels(int level) {
        Level = Math.Clamp(level, 0, MaximumLevel);
        Sync();
    }

    public bool SpendLevels(int amount) {
        if (amount < 0 || Level < amount)
            return false;
        if (amount == 0)
            return true;
        Level -= amount;
        Sync();
        return true;
    }

    private void Sync() {
        Entity.Attributes.SetCurrent(LevelAttribute, Level);
        Entity.Attributes.SetCurrent(ExperienceAttribute, Progress);
    }
}
