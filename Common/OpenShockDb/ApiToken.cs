using System.Net;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class ApiToken
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public IPAddress CreatedByIp { get; set; } = null!;

    public DateTime? ValidUntil { get; set; }
    
    public List<PermissionType> Permissions { get; set; } = null!;

    public DateTime LastUsed { get; set; }

    public virtual User User { get; set; } = null!;
}
