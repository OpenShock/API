using OpenShock.Common.Models;

namespace OpenShock.LiveControlGateway.Models;

/// <summary>
/// Permissions and limits for a live shocker
/// </summary>
public sealed class LiveShockerPermission
{
    /// <summary>
    /// Is the live shocker paused
    /// </summary>
    public required bool Paused { get; set; }
    
    /// <summary>
    /// Perms and limits for the live shocker
    /// </summary>
    public required SharePermsAndLimits PermsAndLimits { get; set; }
}