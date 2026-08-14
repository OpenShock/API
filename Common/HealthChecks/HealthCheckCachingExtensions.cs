using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace OpenShock.Common.HealthChecks;

public static class HealthCheckCachingExtensions
{
    /// <summary>
    /// Wraps the registered <see cref="HealthCheckService"/> in a <see cref="CachedHealthCheckService"/>, so
    /// every consumer - the health endpoint and the LCG keep alive gate alike - reads the same report and the
    /// dependencies are probed at most once per <paramref name="ttl"/>.
    /// Call once, after the checks themselves are registered; decorating twice would nest two caches.
    /// </summary>
    public static IServiceCollection AddCachedHealthChecks(this IServiceCollection services, TimeSpan ttl)
    {
        // Hosts that register no checks of their own still resolve the service, and Decorate throws on an
        // unregistered one, so make sure it exists first.
        services.AddHealthChecks();

        // The TTL is not a resolvable dependency, so this takes the factory overload rather than
        // Decorate<HealthCheckService, CachedHealthCheckService>().
        // IDISP005: the decorator is disposable but HealthCheckService is not, so the factory cannot say so.
        // The container owns the singleton it builds here and disposes it with the provider.
#pragma warning disable IDISP005
        services.Decorate<HealthCheckService>((inner, sp) => new CachedHealthCheckService(
            inner,
            sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>(),
            ttl));
#pragma warning restore IDISP005

        return services;
    }
}