namespace Basalt.Core.Blocks.Types;

public sealed class ItemDrop
{
    public string Type { get; }
    public int Min { get; }
    public int Max { get; }
    public double Probability { get; }

    public ItemDrop(string type, int min, int max, double probability)
    {
        if (min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "Min cannot be greater than Max.");
        }

        if (probability < 0d || probability > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0.");
        }

        Type = type;
        Min = min;
        Max = max;
        Probability = probability;
    }

    public int Roll()
    {
        if (Random.Shared.NextDouble() > Probability)
        {
            return 0;
        }

        return Random.Shared.Next(Min, Max + 1);
    }
}







