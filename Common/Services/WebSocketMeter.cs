using System.Diagnostics.Metrics;

namespace OpenShock.Common.Services;

public sealed class WebSocketMeter : IWebSocketMeter
{
    private readonly Histogram<int> _msgSize;

    public WebSocketMeter(IMeterFactory meterFactory)
    {
        // IMeterFactory automatically manages the lifetime of any Meter objects it creates, disposing them when the DI container is disposed.
        // It's unnecessary to add extra code to invoke Dispose() on the Meter, and it won't have any effect.
        // Source: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation#best-practices-1
#pragma warning disable IDISP001
        var meter = meterFactory.Create("OpenShock.WebSocket");
#pragma warning restore IDISP001

        _msgSize = meter.CreateHistogram<int>(
            "openshock.websocket.message.size",
            unit: "bytes",
            description: "WebSocket/SignalR message size in bytes");
    }

    public void RegisterMessageSize(int sizeBytes)
    {
        _msgSize.Record(sizeBytes);
    }
}