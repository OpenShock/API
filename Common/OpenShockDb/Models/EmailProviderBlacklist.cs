namespace OpenShock.Common.OpenShockDb;

public sealed class EmailProviderBlacklist
{
    public required Guid Id { get; init; }

    public required string Domain { get; init; }
    
    public DateTime CreatedAt { get; init; }
}