namespace OpenShock.Common.OpenShockDb;

public sealed class EmailProviderBlacklist
{
    public required string Domain { get; set; }
    
    public DateTime CreatedAt { get; set; }
}