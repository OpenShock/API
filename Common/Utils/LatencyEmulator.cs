namespace OpenShock.Common.Utils;

public sealed class LatencyEmulator
{
    // Use object for broad framework compat; replace with `Lock` if desired.
    private readonly Lock _gate = new();
    private readonly long[] _buf;
    private int _count;
    private int _head;

    // Use double to prevent overflow and improve precision of stats.
    private double _sum;
    private double _sumSq;

    /// <summary>
    /// Sliding window of timing samples (stored as ticks).
    /// Seeds the window with one sample = max(defaultMs, 0).
    /// </summary>
    public LatencyEmulator(int capacity, long defaultMs)
    {
        if (capacity <= 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be > 1.");
        
        ArgumentOutOfRangeException.ThrowIfNegative(defaultMs);

        _buf = new long[capacity];

        long ticks = defaultMs * TimeSpan.TicksPerMillisecond;
        _buf[0] = ticks;
        _count = 1;
        _head  = 1;

        _sum   = ticks;
        _sumSq = (double)ticks * ticks;
    }

    /// <summary>
    /// Record a timing sample in TICKS (not milliseconds).
    /// </summary>
    public void Record(long elapsedTicks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(elapsedTicks);

        lock (_gate)
        {
            if (_count < _buf.Length)
            {
                // growing phase: no evictions
                _buf[_head] = elapsedTicks;
                _count++;
            }
            else
            {
                // steady state: evict oldest at _head, then insert
                long old = _buf[_head];
                _sum   -= old;
                _sumSq -= (double)old * old;

                _buf[_head] = elapsedTicks;
            }

            _sum   += elapsedTicks;
            _sumSq += (double)elapsedTicks * elapsedTicks;

            _head = (_head + 1) % _buf.Length;
        }
    }

    /// <summary>
    /// Return a simulated timing using current window mean ± Gaussian noise (as a TimeSpan).
    /// Clamped to non-negative ticks.
    /// </summary>
    public TimeSpan GetFake()
    {
        lock (_gate)
        {
            var (mean, std) = MeanStdUnsafe();
            double noise = std > 0 ? NextGaussian(0, std) : 0;
            double value = Math.Max(mean + noise, 0); // clamp at 0
            return TimeSpan.FromTicks((long)Math.Round(value));
        }
    }

    /// <summary>
    /// Returns (meanMs, stdDevMs)
    /// </summary>
    public (double mean, double std) GetStats()
    {
        double mean, std;
        
        lock (_gate)
        {
            (mean, std) = MeanStdUnsafe();
        }
        
        return (mean / TimeSpan.TicksPerMillisecond, std / TimeSpan.TicksPerMillisecond);
    }

    // --- helpers ---

    // Uses maintained sums for O(1) stats
    private (double mean, double std) MeanStdUnsafe()
    {
        switch (_count)
        {
            case 0:
                return (0, 0);
            case 1:
                double v = _buf[0];
                return (v, 0);
        }

        double n = _count;
        double mean = _sum / n;
        // Unbiased sample variance
        double variance = (_sumSq - n * mean * mean) / (n - 1);
        double std = Math.Sqrt(Math.Max(variance, 0));
        return (mean, std);
    }

    private static double NextGaussian(double mean, double stdDev)
    {
        // Box–Muller with Random.Shared
        double u1 = 1.0 - Random.Shared.NextDouble(); // (0,1]
        double u2 = 1.0 - Random.Shared.NextDouble();
        double mag = Math.Sqrt(-2.0 * Math.Log(u1));
        double z0 = mag * Math.Cos(2.0 * Math.PI * u2);
        return mean + z0 * stdDev;
    }
}
