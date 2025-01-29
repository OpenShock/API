using System.Net;

namespace OpenShock.Common.OpenShockDb;

public class ApiTokenReport
{
    public Guid Id { get; set; }

    public DateTime ReportedAt { get; set; }

    public Guid ReportedByUserId { get; set; }

    public IPAddress ReportedByIp { get; set; } = null!;

    public virtual User ReportedByUser { get; set; } = null!;
}
