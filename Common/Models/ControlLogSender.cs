namespace OpenShock.Common.Models;

public class ControlLogSenderLight
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Uri Image { get; init; }
    public required string? CustomName { get; init; }
}

public sealed class ControlLogSender : ControlLogSenderLight
{
    public required string ConnectionId { get; init; }
    public required Dictionary<string, object> AdditionalItems { get; init; }
}
