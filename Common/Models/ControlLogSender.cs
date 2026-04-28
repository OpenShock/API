namespace OpenShock.Common.Models;

public class ControlLogSenderLight
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Uri Image { get; set; }
    public required string? CustomName { get; set; }
}

public class ControlLogSender : ControlLogSenderLight
{
    public required string ConnectionId { get; set; }
    public required Dictionary<string, object> AdditionalItems { get; set; }
}