using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class ShockerLogsResponse
{
    public required ICollection<LogEntryV2> Logs { get; init; }
}

public sealed class LogEntryV2
{
    public required Guid Id { get; init; }

    public required Guid DeviceId { get; init; }
    public required string DeviceName { get; init; }

    public required Guid ShockerId { get; init; }
    public required string ShockerName { get; init; }

    public required DateTime CreatedOn { get; init; }

    public required ControlType Type { get; init; }

    public required ControlLogSenderLight ControlledBy { get; init; }

    public required byte Intensity { get; init; }

    public required uint Duration { get; init; }
}