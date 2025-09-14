using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class LatencyEmulatorTests
{
    // --- helpers ---
    private static long Ms(double ms) => (long)(ms * TimeSpan.TicksPerMillisecond);

    private static (double mean, double std) MeanStd(IEnumerable<long> samples)
    {
        var arr = samples.Select(x => (double)x).ToArray();
        double n = arr.Length;
        double mean = arr.Average();
        if (n <= 1) return (mean, 0);

        double sumSq = arr.Sum(x => x * x);
        double variance = (sumSq - n * mean * mean) / (n - 1);
        return (mean, Math.Sqrt(Math.Max(0, variance)));
    }

    private const double Eps = 1e-7;

    // --- constructor & basic stats ---

    [Test]
    public async Task Ctor_SeedsWithDefaultMs_StatsMatch()
    {
        var seedMs = 12;
        var emu = new LatencyEmulator(capacity: 8, defaultMs: seedMs);

        var (mean, std) = emu.GetStats();
        await Assert.That(mean).IsEqualTo(seedMs).Within(0.5);
        await Assert.That(std).IsEqualTo(0);
    }

    [Test]
    public void Ctor_DefaultMs_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatencyEmulator(capacity: 8, -1));
    }

    [Test]
    public void Ctor_Capacity_OneOrLess_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatencyEmulator(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatencyEmulator(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatencyEmulator(-5, 0));
    }

    // --- Record: input validation ---

    [Test]
    public void Record_Negative_Throws()
    {
        var emu = new LatencyEmulator(capacity: 4, defaultMs: 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => emu.Record(-1));
    }

    [Test]
    public void Record_Zero_Throws()
    {
        var emu = new LatencyEmulator(capacity: 4, defaultMs: 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => emu.Record(0));
    }

    // --- Record: growth phase (no eviction) ---

    [Test]
    public async Task Record_Growing_NoEvictions_StatsMatchAllSamples()
    {
        var emu = new LatencyEmulator(capacity: 8, defaultMs: 0);

        // Add positive tick samples
        long[] add = [ 1, 3, 5 ];
        foreach (var t in add) emu.Record(Ms(t));

        // window should contain [0, 1 ms, 3 ms, 5 ms] (ticks)
        var expected = new List<long> { 0 };
        expected.AddRange(add);

        var (expMean, expStd) = MeanStd(expected);
        var (mean, std) = emu.GetStats();

        await Assert.That(mean).IsEqualTo(expMean).Within(Eps);
        await Assert.That(std).IsEqualTo(expStd).Within(Eps);
    }

    // --- Record: steady state (with eviction) ---

    [Test]
    public async Task Record_EvictsOldest_MaintainsSlidingWindow()
    {
        // capacity 3, seed 0 ms => window starts [0]
        var emu = new LatencyEmulator(capacity: 3, defaultMs: 0);

        // Fill to capacity: [0, 10 ms, 20 ms]
        emu.Record(Ms(10));
        emu.Record(Ms(20));

        var (m1, s1) = emu.GetStats();
        long[] expected1 = [ 0, 10, 20 ];
        var (expM1, expS1) = MeanStd(expected1);
        await Assert.That(m1).IsEqualTo(expM1).Within(Eps);
        await Assert.That(s1).IsEqualTo(expS1).Within(Eps);

        // Next insert 30 ms => evict the oldest (0), new window [10,20,30]
        emu.Record(Ms(30));
        var (m2, s2) = emu.GetStats();
        long[] expected2 = [ 10, 20, 30 ];
        var (expM2, expS2) = MeanStd(expected2);
        await Assert.That(m2).IsEqualTo(expM2).Within(Eps);
        await Assert.That(s2).IsEqualTo(expS2).Within(Eps);

        // Next insert 40 ms => window [20,30,40]
        emu.Record(Ms(40));
        var (m3, s3) = emu.GetStats();
        long[] expected3 = [ 20, 30, 40 ];
        var (expM3, expS3) = MeanStd(expected3);
        await Assert.That(m3).IsEqualTo(expM3).Within(Eps);
        await Assert.That(s3).IsEqualTo(expS3).Within(Eps);
    }

    // --- GetFake() behavior ---

    [Test]
    public async Task GetFake_WhenStdZero_ReturnsMeanExactly()
    {
        // Make all samples identical so std==0
        var emu = new LatencyEmulator(capacity: 5, defaultMs: 7);
        var same = Ms(7.0);
        emu.Record(same);
        emu.Record(same);
        emu.Record(same);
        emu.Record(same);

        var (mean, std) = emu.GetStats();
        await Assert.That(std).IsEqualTo(0);

        // Without noise, fake should be exactly the mean (with rounding)
        var fake = emu.GetFake();
        await Assert.That(fake.TotalMilliseconds).IsEqualTo(mean).Within(Eps);
    }

    [Test]
    public async Task GetFake_NonZeroStd_NonNegative_AndVaries()
    {
        var emu = new LatencyEmulator(capacity: 16, defaultMs: 0);

        // Create a spread so std>0
        foreach (var ms in new[] { 1, 2, 3, 5, 8, 13, 21, 34 }) emu.Record(Ms(ms));

        var (mean, std) = emu.GetStats();
        await Assert.That(std).IsGreaterThan(0);

        // Gather many samples; all must be non-negative,
        // and at least one should differ from rounded mean.
        var fakes = new List<long>();
        for (int i = 0; i < 200; i++) fakes.Add(emu.GetFake().Ticks);

        await Assert.That(fakes).DoesNotContain(x => x < 0);
        await Assert.That(fakes).ContainsOnly(x => Math.Abs(x - Math.Round(mean)) > 0);
    }

    // --- Numerical stability & precision ---

    [Test]
    public async Task Stats_UnbiasedSampleStd_MatchesReference()
    {
        var emu = new LatencyEmulator(capacity: 8, defaultMs: 0);
        long[] vals = [ Ms(10), Ms(20), Ms(30), Ms(40), Ms(50) ];
        foreach (var v in vals) emu.Record(Ms(v));

        // Window: [0,10,20,30,40,50] (n=6) all in ms converted to ticks
        var expected = new long[] { 0 }.Concat(vals).ToArray();
        var (expMean, expStd) = MeanStd(expected);

        var (mean, std) = emu.GetStats();
        await Assert.That(mean).IsEqualTo(expMean).Within(Eps);
        await Assert.That(std).IsEqualTo(expStd).Within(Eps);
    }

    // --- Concurrency sanity check (no exceptions, stats sane) ---

    [Test]
    public async Task Record_IsThreadSafe_Sanity()
    {
        var emu = new LatencyEmulator(capacity: 128, defaultMs: 1);

        var tasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(i => Task.Run(() =>
            {
                var rnd = new Random(i * 7919 + 17);
                for (int k = 0; k < 5000; k++)
                {
                    // Generate strictly positive tick values (~ up to 10ms)
                    // Ensure >= 1 tick to satisfy ThrowIfNegativeOrZero.
                    long ticks = Math.Max(1, TimeSpan.FromMilliseconds(rnd.NextDouble() * 10).Ticks);
                    emu.Record(ticks);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var (mean, std) = emu.GetStats();
        // Just make sure we didn’t corrupt numeric state.
        await Assert.That(double.IsNaN(mean) || double.IsInfinity(mean)).IsFalse();
        await Assert.That(double.IsNaN(std) || double.IsInfinity(std)).IsFalse();
        await Assert.That(mean).IsGreaterThan(0);
        await Assert.That(std).IsGreaterThanOrEqualTo(0);
    }
}
