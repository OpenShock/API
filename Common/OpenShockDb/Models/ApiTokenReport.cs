using System.Net;

namespace OpenShock.Common.OpenShockDb;

public class ApiTokenReport
{
    public required Guid Id { get; set; }
    
    public required int SubmittedCount { get; set; }
    
    public required int AffectedCount { get; set; }

    public required Guid UserId { get; set; }

    public required IPAddress IpAddress { get; set; }

    public required string? IpCountry { get; set; }

    public DateTime CreatedAt { get; set; }

    public User ReportedByUser { get; set; } = null!;
}
