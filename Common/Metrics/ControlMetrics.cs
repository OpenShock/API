using System.Diagnostics.Metrics;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Metrics;

/// <summary>
/// Instruments for shocker control, the product's central action. Owned by a singleton because
/// <see cref="Services.ControlSender"/> is scoped: instruments created per request would be
/// re-created on every call.
/// </summary>
public sealed class ControlMetrics
{
    /// <summary>
    /// Meter name, registered with OpenTelemetry in <c>AddOpenShockServices</c>.
    /// </summary>
    public const string MeterName = "OpenShock.Common";

    /// <summary>Where a control request came from.</summary>
    public static class Source
    {
        /// <summary>A user controlling their own or shared shockers.</summary>
        public const string User = "user";

        /// <summary>A public share link.</summary>
        public const string PublicShare = "public_share";
    }

    /// <summary>How a control request ended.</summary>
    public static class Outcome
    {
        /// <summary>Every command was dispatched.</summary>
        public const string Success = "success";

        /// <summary>A named shocker does not exist or the caller has no access to it.</summary>
        public const string ShockerNotFound = "shocker_not_found";

        /// <summary>A named shocker is paused.</summary>
        public const string ShockerPaused = "shocker_paused";

        /// <summary>The caller's share does not permit the requested control type.</summary>
        public const string NoPermission = "no_permission";
    }

    private readonly Counter<long> _requests;
    private readonly Counter<long> _commands;
    private readonly Histogram<int> _shockersPerRequest;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="meterFactory"></param>
    public ControlMetrics(IMeterFactory meterFactory)
    {
        // The factory owns the meters it hands out and disposes them with the container. It also
        // caches by name, so disposing this one would tear it out from under every other consumer.
#pragma warning disable IDISP001
        var meter = meterFactory.Create(MeterName);
#pragma warning restore IDISP001

        _requests = meter.CreateCounter<long>("openshock_control_requests", "requests",
            "Shocker control requests by source and outcome.");

        // Counted per command rather than per request, because a request carrying five shocks is five
        // shocks. Only dispatched commands are counted: a request is rejected on its first bad shocker,
        // so the ones built before that point never reach a hub.
        _commands = meter.CreateCounter<long>("openshock_control_commands", "commands",
            "Individual shocker commands dispatched, by control type.");

        // A request is rejected on its first bad shocker, so this only records dispatched requests -
        // it is the fan-out of a successful control, not the size of what was asked for.
        // Name ends in the unit on purpose: the Prometheus exporter appends the unit to the metric
        // name unless it is already the suffix, so "..._shockers_per_request" would scrape as
        // "openshock_control_shockers_per_request_shockers".
        _shockersPerRequest = meter.CreateHistogram<int>("openshock_control_request_shockers", "shockers",
            "Distinct shockers commanded by a single successful control request.",
            advice: new InstrumentAdvice<int>
            {
                HistogramBucketBoundaries = [1, 2, 3, 5, 8, 12, 20, 35, 50, 100]
            });
    }

    /// <summary>
    /// Record a rejected control request.
    /// </summary>
    /// <param name="source">One of <see cref="Source"/></param>
    /// <param name="outcome">One of <see cref="Outcome"/></param>
    public void Rejected(string source, string outcome) => Record(source, outcome);

    /// <summary>
    /// Record a control request whose commands were all dispatched.
    /// </summary>
    /// <param name="source">One of <see cref="Source"/></param>
    /// <param name="shockerCount">Distinct shockers commanded</param>
    public void Dispatched(string source, int shockerCount)
    {
        Record(source, Outcome.Success);
        _shockersPerRequest.Record(shockerCount, new KeyValuePair<string, object?>("source", source));
    }

    /// <summary>
    /// Record a single dispatched shocker command.
    /// </summary>
    /// <param name="source">One of <see cref="Source"/></param>
    /// <param name="type">The control type that was sent</param>
    public void Command(string source, ControlType type) =>
        _commands.Add(1,
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("type", ControlTypeTag.Of(type)));

    private void Record(string source, string outcome) =>
        _requests.Add(1,
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("outcome", outcome));
}
