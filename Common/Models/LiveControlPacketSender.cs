namespace OpenShock.Common.Models;

public sealed class LiveControlPacketSender
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Uri Image { get; init; }
    public required string? CustomName { get; init; }
}
