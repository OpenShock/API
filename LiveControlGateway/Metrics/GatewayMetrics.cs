using System.Diagnostics.Metrics;
using OpenShock.Common.Metrics;
using OpenShock.Common.OpenShockDb;
using OpenShock.LiveControlGateway.Options;

namespace OpenShock.LiveControlGateway.Metrics;

/// <summary>
/// The gateway's instruments, owned by a singleton because the things being measured are not:
/// a <see cref="Controllers.LiveControlController"/> exists per websocket connection, so instruments
/// created there would be re-created (and re-registered) on every connect.
/// </summary>
/// <remarks>
/// Every measurement carries <c>gateway_fqdn</c> as a normal tag rather than a tag on the
/// <see cref="Meter"/> itself - a meter-level tag is a *scope* attribute, which the Prometheus
/// exporter emits prefixed as <c>otel_scope_gateway_fqdn</c>.
/// </remarks>
public sealed class GatewayMetrics
{
    /// <summary>Outcomes of a hub trying to establish its gateway connection.</summary>
    public static class HubConnectOutcome
    {
        /// <summary>A hub with no existing lifetime connected and initialized.</summary>
        public const string Connected = "connected";

        /// <summary>A hub that already had a lifetime reconnected, swapping onto the existing one.</summary>
        public const string Swapped = "swapped";

        /// <summary>The existing lifetime could not be swapped or was already going away.</summary>
        public const string Busy = "busy";

        /// <summary>The fresh lifetime failed to initialize and was torn down again.</summary>
        public const string InitFailed = "init_failed";
    }

    /// <summary>Outcomes of a client trying to open a live control session against a hub.</summary>
    public static class LiveControlOutcome
    {
        /// <summary>The client was attached to the hub's lifetime.</summary>
        public const string Connected = "connected";

        /// <summary>No hub with that id is connected to this gateway.</summary>
        public const string HubNotFound = "hub_not_found";

        /// <summary>The hub's lifetime is being torn down and cannot take new clients.</summary>
        public const string HubBusy = "hub_busy";
    }

    /// <summary>Outcomes of a single live control frame, whether it arrived alone or inside a bulk frame.</summary>
    public static class FrameOutcome
    {
        /// <summary>The frame was handed to the hub lifetime.</summary>
        public const string Accepted = "accepted";

        /// <summary>The API token driving the session is paused.</summary>
        public const string TokenPaused = "token_paused";

        /// <summary>The frame named a shocker this session does not have.</summary>
        public const string ShockerNotFound = "shocker_not_found";

        /// <summary>The share does not permit live control.</summary>
        public const string LiveNotEnabled = "live_not_enabled";

        /// <summary>The share does not permit this control type.</summary>
        public const string NoPermission = "no_permission";

        /// <summary>The shocker is paused.</summary>
        public const string ShockerPaused = "shocker_paused";

        /// <summary>Another session holds the shocker exclusively.</summary>
        public const string ShockerExclusive = "shocker_exclusive";
    }

    private readonly KeyValuePair<string, object?> _gatewayFqdn;

    private readonly Counter<long> _hubConnectAttempts;
    private readonly Counter<long> _hubDisconnections;
    private readonly Counter<long> _liveControlAttempts;
    private readonly Histogram<int> _liveControlLatency;
    private readonly Counter<long> _frames;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="lcgOptions"></param>
    /// <param name="meter"></param>
    public GatewayMetrics(LcgOptions lcgOptions, [FromKeyedServices("OpenShock.Gateway.Meter")] Meter meter)
    {
        _gatewayFqdn = new KeyValuePair<string, object?>("gateway_fqdn", lcgOptions.Fqdn);

        _hubConnectAttempts = meter.CreateCounter<long>("openshock_hub_connect_attempts", "attempts",
            "Hub connection attempts by outcome. A gauge of live connections hides flapping; this does not.");

        _hubDisconnections = meter.CreateCounter<long>("openshock_hub_disconnections", "disconnections",
            "Hub lifetimes torn down. Paired with the connect attempts this gives connection churn.");

        _liveControlAttempts = meter.CreateCounter<long>("openshock_live_control_attempts", "attempts",
            "Live control session attempts by outcome.");

        // Explicit boundaries: this is client-to-gateway round trip over the public internet, so the
        // interesting resolution sits well below the SDK's default first buckets.
        _liveControlLatency = meter.CreateHistogram<int>("openshock_live_control_latency", "ms",
            "Round trip latency to live control clients, measured from the ping/pong exchange.",
            advice: new InstrumentAdvice<int>
            {
                HistogramBucketBoundaries = [5, 10, 20, 30, 50, 75, 100, 150, 200, 300, 500, 1000, 2000]
            });

        // Tagged by control type so live control shows up next to the API's command counter, but the
        // two do not mean the same thing: a live session streams frames at the hub's tick rate, so
        // these count ticks of a held control, not discrete presses.
        _frames = meter.CreateCounter<long>("openshock_live_frames", "frames",
            "Live control frames processed by outcome and control type, counted per frame (a bulk frame counts once per frame it carries).");
    }

    /// <summary>
    /// Record the outcome of a hub connection attempt.
    /// </summary>
    /// <param name="outcome">One of <see cref="HubConnectOutcome"/></param>
    public void HubConnectAttempt(string outcome) =>
        _hubConnectAttempts.Add(1, _gatewayFqdn, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// Record a hub lifetime being torn down.
    /// </summary>
    public void HubDisconnected() => _hubDisconnections.Add(1, _gatewayFqdn);

    /// <summary>
    /// Record the outcome of a live control session attempt. This is churn only - the number of
    /// sessions currently open is an observable gauge over the hub lifetimes, in
    /// <see cref="LifetimeManager.HubLifetimeManager"/>.
    /// </summary>
    /// <param name="outcome">One of <see cref="LiveControlOutcome"/></param>
    public void LiveControlAttempt(string outcome) =>
        _liveControlAttempts.Add(1, _gatewayFqdn, new KeyValuePair<string, object?>("outcome", outcome));

    /// <summary>
    /// Record a measured client round trip.
    /// </summary>
    /// <param name="latencyMs"></param>
    public void LiveControlLatency(int latencyMs) => _liveControlLatency.Record(latencyMs, _gatewayFqdn);

    /// <summary>
    /// Record a processed live control frame.
    /// </summary>
    /// <param name="outcome">One of <see cref="FrameOutcome"/></param>
    /// <param name="type">The control type the frame carried</param>
    public void Frame(string outcome, ControlType type) =>
        _frames.Add(1, _gatewayFqdn,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("type", ControlTypeTag.Of(type)));
}
