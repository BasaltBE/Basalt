using System.Diagnostics;

namespace BasaltBE.Basalt.Profiling;

public sealed class TickMetrics
{
    private const int Capacity = 100;
    private readonly long[] starts = new long[Capacity];
    private readonly long[] durations = new long[Capacity];
    private long total;
    private int count;
    private int next;

    public double LastMilliseconds => count == 0 ? 0 : durations[(next + Capacity - 1) % Capacity] * 1000.0 / Stopwatch.Frequency;
    public double AverageMilliseconds => count == 0 ? 0 : total * 1000.0 / Stopwatch.Frequency / count;

    public double Tps
    {
        get
        {
            if (count < 2)
                return 0;
            long elapsed = starts[(next + Capacity - 1) % Capacity] - starts[count == Capacity ? next : 0];
            return elapsed > 0 ? (count - 1) * (double)Stopwatch.Frequency / elapsed : 0;
        }
    }

    public double MaximumMilliseconds
    {
        get
        {
            long maximum = 0;
            for (int index = 0; index < count; index++)
                maximum = Math.Max(maximum, durations[index]);
            return maximum * 1000.0 / Stopwatch.Frequency;
        }
    }

    internal void Record(long start)
    {
        long duration = Stopwatch.GetTimestamp() - start;
        total += duration - durations[next];
        starts[next] = start;
        durations[next] = duration;
        count = Math.Min(count + 1, Capacity);
        if (++next == Capacity)
            next = 0;
    }
}
