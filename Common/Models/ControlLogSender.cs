namespace OpenShock.Common.Models;

public class ControlLogSender : ControlLogSenderLight
{
    public required string ConnectionId { get; set; }
    public required IDictionary<string, object> AdditionalItems { get; set; }
}

public class ControlLogSenderLight : GenericIni
{
    public required string? CustomName { get; set; }
}