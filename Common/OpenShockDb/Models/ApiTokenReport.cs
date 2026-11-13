using System.Net;

namespace OpenShock.Common.OpenShockDb;

public class ApiTokenReport
{
    public Guid Id { get; init; }
    
    public required int SubmittedCount { get; init; }
    
    public required int AffectedCount { get; init; }

    public required Guid UserId { get; init; }

    public required IPAddress IpAddress { get; init; }

    public required string? IpCountry { get; init; }

    public DateTime CreatedAt { get; init; }

    public User ReportedByUser { get; init; } = null!;
}
