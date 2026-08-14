using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenShock.Common.HealthChecks;
// "Options" alone binds to the OpenShock.Common.Options namespace from inside OpenShock.Common.*.
using MsOptions = Microsoft.Extensions.Options.Options;

namespace OpenShock.Common.Tests.HealthChecks;

public class CachedHealthCheckServiceTests
{
    // --- helpers ---

    private static HealthCheckRegistration Registration(string name, params string[] tags) =>
        new(name, new StubHealthCheck(), HealthStatus.Unhealthy, tags);

    private static IOptions<HealthCheckServiceOptions> OptionsFor(params HealthCheckRegistration[] registrations)
    {
        var options = new HealthCheckServiceOptions();
        foreach (var registration in registrations) options.Registrations.Add(registration);

        return MsOptions.Create(options);
    }

    private static HealthReport Report(params (string Name, HealthStatus Status)[] entries) =>
        new(entries.ToDictionary(
                entry => entry.Name,
                entry => new HealthReportEntry(entry.Status, null, TimeSpan.Zero, null, null),
                StringComparer.OrdinalIgnoreCase),
            TimeSpan.Zero);

    private sealed class StubHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    /// <summary>
    /// Stands in for the real service so a test can count how often the checks were actually run.
    /// </summary>
    private sealed class CountingHealthCheckService : HealthCheckService
    {
        private readonly HealthReport _report;
        private readonly TimeSpan _delay;

        private int _calls;
        public int Calls => Volatile.Read(ref _calls);

        public CountingHealthCheckService(HealthReport report, TimeSpan delay = default)
        {
            _report = report;
            _delay = delay;
        }

        public override async Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            if (_delay > TimeSpan.Zero) await Task.Delay(_delay, cancellationToken);

            return _report;
        }
    }

    // --- caching ---

    [Test]
    public async Task CheckHealth_WithinTtl_DoesNotRerunChecks()
    {
        var inner = new CountingHealthCheckService(Report(("database", HealthStatus.Healthy)));
        using var cached = new CachedHealthCheckService(inner, OptionsFor(Registration("database")), TimeSpan.FromMinutes(1));

        await cached.CheckHealthAsync(null);
        await cached.CheckHealthAsync(null);
        await cached.CheckHealthAsync(null);

        await Assert.That(inner.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task CheckHealth_AfterTtl_RerunsChecks()
    {
        var inner = new CountingHealthCheckService(Report(("database", HealthStatus.Healthy)));
        using var cached = new CachedHealthCheckService(inner, OptionsFor(Registration("database")), TimeSpan.FromMilliseconds(50));

        await cached.CheckHealthAsync(null);
        await Task.Delay(150);
        await cached.CheckHealthAsync(null);

        await Assert.That(inner.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task CheckHealth_ConcurrentOnColdCache_RunsChecksOnce()
    {
        var inner = new CountingHealthCheckService(Report(("database", HealthStatus.Healthy)), TimeSpan.FromMilliseconds(100));
        using var cached = new CachedHealthCheckService(inner, OptionsFor(Registration("database")), TimeSpan.FromMinutes(1));

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => cached.CheckHealthAsync(null)));

        await Assert.That(inner.Calls).IsEqualTo(1);
    }

    // --- predicate handling ---

    [Test]
    public async Task CheckHealth_WithPredicate_ReturnsOnlySelectedEntries()
    {
        var inner = new CountingHealthCheckService(
            Report(("database", HealthStatus.Healthy), ("scratch", HealthStatus.Healthy)));
        using var cached = new CachedHealthCheckService(
            inner,
            OptionsFor(Registration("database", "ready"), Registration("scratch")),
            TimeSpan.FromMinutes(1));

        var report = await cached.CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        await Assert.That(report.Entries.Keys).IsEquivalentTo(new[] { "database" });
    }

    [Test]
    public async Task CheckHealth_WithPredicate_RecomputesStatusFromSelectedEntries()
    {
        // The unselected check is the failing one, so the subset must come back Healthy even though the
        // full report it was cut from is not.
        var inner = new CountingHealthCheckService(
            Report(("database", HealthStatus.Healthy), ("scratch", HealthStatus.Unhealthy)));
        using var cached = new CachedHealthCheckService(
            inner,
            OptionsFor(Registration("database", "ready"), Registration("scratch")),
            TimeSpan.FromMinutes(1));

        var full = await cached.CheckHealthAsync(null);
        var ready = await cached.CheckHealthAsync(registration => registration.Tags.Contains("ready"));

        await Assert.That(full.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(ready.Status).IsEqualTo(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealth_DifferentPredicates_ShareOneRun()
    {
        var inner = new CountingHealthCheckService(
            Report(("database", HealthStatus.Healthy), ("scratch", HealthStatus.Healthy)));
        using var cached = new CachedHealthCheckService(
            inner,
            OptionsFor(Registration("database", "ready"), Registration("scratch")),
            TimeSpan.FromMinutes(1));

        await cached.CheckHealthAsync(registration => registration.Tags.Contains("ready"));
        await cached.CheckHealthAsync(null);

        await Assert.That(inner.Calls).IsEqualTo(1);
    }

    // --- wiring ---

    [Test]
    public async Task Decoration_ResolvesCachedServiceOverTheBuiltInOne()
    {
        // Guards the decoration itself: the service being wrapped is an internal framework type, so this
        // only works if Scrutor can construct it from the descriptor it replaced.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddCheck("database", new StubHealthCheck(), tags: ["ready"]);
        services.AddCachedHealthChecks(TimeSpan.FromMinutes(1));

        await using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<HealthCheckService>();

        await Assert.That(service).IsTypeOf<CachedHealthCheckService>();

        var report = await service.CheckHealthAsync(registration => registration.Tags.Contains("ready"));
        await Assert.That(report.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(report.Entries.Keys).IsEquivalentTo(new[] { "database" });
    }
}