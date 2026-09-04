using Basalt.BedrockProtocol.NBT;
using System.Reflection;

namespace Basalt.Core.Traits;

public abstract class Trait {
    private uint _randomTickNumerator = 1;
    private uint _randomTickDenominator = 4096;
    private readonly bool _hasTickHandler;
    private readonly bool _hasRandomTickHandler;

    protected Trait() {
        _hasTickHandler = GetType()
            .GetMethod(nameof(OnTick), BindingFlags.Instance | BindingFlags.Public)
            ?.DeclaringType != typeof(Trait);
        _hasRandomTickHandler = GetType()
            .GetMethod(nameof(OnRandomTick), BindingFlags.Instance | BindingFlags.Public)
            ?.DeclaringType != typeof(Trait);
    }

    internal bool HasTickHandler => _hasTickHandler;
    internal bool HasRandomTickHandler => _hasRandomTickHandler;

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

        ulong threshold = (ulong)_randomTickNumerator * factor;
        if (threshold >= _randomTickDenominator)
            return true;

        if (_randomTickDenominator <= int.MaxValue) {
            return Random.Shared.Next((int)_randomTickDenominator) < (int)threshold;
        }

        return Random.Shared.NextDouble() < (double)threshold / _randomTickDenominator;
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






