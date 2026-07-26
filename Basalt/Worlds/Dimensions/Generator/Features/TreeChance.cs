namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public readonly struct TreeChance {
    public readonly int Numerator;
    public readonly int Denominator;

    public TreeChance(int numerator, int denominator) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(denominator, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(numerator);

        if (numerator > denominator) {
            throw new ArgumentOutOfRangeException(nameof(numerator));
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    internal bool Roll(Random random) {
        return Numerator == Denominator ||
            Numerator > 0 && random.Next(Denominator) < Numerator;
    }
}
