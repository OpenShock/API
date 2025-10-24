using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class ShockerLogEntry
{
    public Guid Id { get; set; }
    public Guid ShockerId { get; set; }

    public ushort Duration { get; set; }
    public byte Intensity { get; set; }
    public ControlType Type { get; set; }

    public DateTime CreatedOn { get; set; }

    public ControlLogSenderLight ControlledBy { get; set; } = default!;
}