using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class LogEntry
{
    public required Guid Id { get; set; }

    public required DateTime CreatedOn { get; set; }

    public required ControlType Type { get; set; }

    public required ControlLogSenderLight ControlledBy { get; set; }

    public required byte Intensity { get; set; }

    public required uint Duration { get; set; }
}