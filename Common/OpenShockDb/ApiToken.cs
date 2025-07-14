using System.Net;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class ApiToken
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string Name { get; set; }

    public required string TokenHash { get; set; }

    public required IPAddress CreatedByIp { get; set; }

    public required List<PermissionType> Permissions { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastUsed { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
