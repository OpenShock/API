using System.Diagnostics.Metrics;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Computes the load (0-100) this gateway advertises for hub assignment: the highest of hub connection,
/// CPU and memory utilization, since a gateway out of any one of them is full.
/// </summary>
public sealed class GatewayLoadSampler : IDisposable
{
    /// <summary>
    /// Meter the CPU and memory utilization instruments are published on.
    /// </summary>
    public const string ResourceMonitoringMeter = "Microsoft.Extensions.Diagnostics.ResourceMonitoring";

    private const string ContainerCpu = "container.cpu.limit.utilization";
    private const string ContainerMemory = "container.memory.limit.utilization";

    // Fallbacks for Windows outside a container
    private const string ProcessCpu = "process.cpu.utilization";
    private const string ProcessMemory = "dotnet.process.memory.virtual.utilization";

    private readonly HubLifetimeManager _hubLifetimeManager;
    private readonly LcgOptions _options;
    private readonly MeterListener _listener;

    private double? _containerCpu, _containerMemory, _processCpu, _processMemory;

    private volatile byte _last;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="options"></param>
    /// <param name="meter"></param>
    public GatewayLoadSampler(HubLifetimeManager hubLifetimeManager, LcgOptions options,
        [FromKeyedServices("OpenShock.Gateway.Meter")]
        Meter meter)
    {
        _hubLifetimeManager = hubLifetimeManager;
        _options = options;

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != ResourceMonitoringMeter) return;
                if (instrument.Name is ContainerCpu or ContainerMemory or ProcessCpu or ProcessMemory)
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => Record(instrument.Name, value));
        _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => Record(instrument.Name, value));
        _listener.Start();

        var gatewayFqdn = new KeyValuePair<string, object?>("gateway_fqdn", options.Fqdn);
        meter.CreateObservableGauge("openshock_gateway_load", () =>
        {
            return new[] { new Measurement<int>(_last, gatewayFqdn) };
        }, "percent", "Load advertised to the API for hub assignment");
    }

    private void Record(string instrument, double value)
    {
        switch (instrument)
        {
            case ContainerCpu: _containerCpu = value; break;
            case ContainerMemory: _containerMemory = value; break;
            case ProcessCpu: _processCpu = value; break;
            case ProcessMemory: _processMemory = value; break;
        }
    }

    /// <summary>
    /// Takes a new sample. Not thread safe.
    /// </summary>
    public byte Measure()
    {
        _containerCpu = _containerMemory = _processCpu = _processMemory = null;
        _listener.RecordObservableInstruments();

        var connections = ToPercent((double)_hubLifetimeManager.HubCount / _options.MaxHubConnections);
        var cpu = ToPercent(_containerCpu ?? _processCpu ?? 0);
        var memory = ToPercent(_containerMemory ?? _processMemory ?? 0);

        var load = Math.Max(connections, Math.Max(cpu, memory));
        _last = load;
        return load;
    }

    private static byte ToPercent(double fraction) => (byte)Math.Clamp(Math.Round(fraction * 100), 0, 100);

    /// <inheritdoc />
    public void Dispose() => _listener.Dispose();
}