namespace OpenShock.Common.OpenShockDb;

public sealed class EmailProviderBlacklist
{
    public Guid Id { get; set; }

    public required string Domain { get; set; }
    
    public DateTime CreatedAt { get; set; }
}