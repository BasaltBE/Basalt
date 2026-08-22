using Basalt.BedrockProtocol.NBT;

namespace Basalt.Core.Traits;

public abstract class Trait {
    private uint _randomTickNumerator = 1;
    private uint _randomTickDenominator = 4096;

    public virtual string Identifier => GetType().FullName ?? GetType().Name;

    public virtual void OnAdd() {
    }

    public virtual void OnRemove() {
    }

    public virtual void OnTick(TraitOnTickDetails details) {
    }

    public virtual void OnRandomTick() {
    }

    public virtual void OnRead(CompoundTag tag) {
    }

    public virtual void OnWrite(CompoundTag tag) {
    }

    public abstract Trait Clone(params object?[] args);

    public bool ShouldRandomTick(uint factor = 1) {
        if (_randomTickNumerator == 0)
            return false;

        if (_randomTickNumerator == _randomTickDenominator)
            return true;

        double chance = (double)(_randomTickNumerator * factor) / _randomTickDenominator;
        return Random.Shared.NextDouble() < chance;
    }

    public void SetRandomTickProbability(uint numerator, uint denominator) {
        if (denominator == 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "Denominator must be greater than 0.");

        if (numerator > denominator)
            throw new ArgumentOutOfRangeException(nameof(numerator), "Numerator must be less than or equal to denominator.");

        _randomTickNumerator = numerator;
        _randomTickDenominator = denominator;
    }
}






