using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.ControlStats;

namespace OpenShock.Common.Tests.Services;

public class ControlStatsAccumulatorTests
{
    private static readonly Guid Shocker = Guid.CreateVersion7();
    private static readonly Guid Controller = Guid.CreateVersion7();

    private static ControlStatDelta Delta(DateTime at, ControlType type = ControlType.Shock, long count = 1, byte intensity = 50) =>
        new(Shocker, Controller, at, type, Live: true, count, DurationMs: 100, IntensityMs: intensity * 100L, intensity);

    [Test]
    public async Task Add_MergesWithinTheSameHour()
    {
        var accumulator = new ControlStatsAccumulator();
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc), count: 1, intensity: 20));
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 10, 55, 0, DateTimeKind.Utc), count: 0, intensity: 80));

        var merged = (await Assert.That(accumulator.DequeueAll()).HasSingleItem())!;

        await Assert.That(merged.Bucket).IsEqualTo(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        await Assert.That(merged.Count).IsEqualTo(1);
        await Assert.That(merged.DurationMs).IsEqualTo(200);
        await Assert.That(merged.IntensityMs).IsEqualTo(10_000);
        await Assert.That(merged.MaxIntensity).IsEqualTo((byte)80);
    }

    [Test]
    public async Task Add_SplitsAcrossHoursAndTypes_AndIgnoresStop()
    {
        var accumulator = new ControlStatsAccumulator();
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 10, 59, 0, DateTimeKind.Utc)));
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)));
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc), ControlType.Vibrate));
        accumulator.Add(Delta(new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc), ControlType.Stop));

        await Assert.That(accumulator.DequeueAll().Length).IsEqualTo(3);
    }

    [Test]
    public async Task DequeueAll_Drains()
    {
        var accumulator = new ControlStatsAccumulator();
        accumulator.Add(Delta(DateTime.UtcNow));

        await Assert.That(accumulator.DequeueAll().Length).IsEqualTo(1);
        await Assert.That(accumulator.DequeueAll()).IsEmpty();
    }
}
