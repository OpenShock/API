using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class OAuthFinalizeRequest
{
    /// <summary>
    /// Desired username. If omitted, a name will be generated from the external profile.
    /// </summary>
    [Username(true)]
    public required string? Username { get; init; }
    
    [EmailAddress(true)]
    public required string? Email { get; init; }
}