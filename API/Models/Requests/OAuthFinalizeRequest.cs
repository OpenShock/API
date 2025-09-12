using OpenShock.API.OAuth;
using OpenShock.Common.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class OAuthFinalizeRequest
{
    /// <summary>Action to perform: "create" or "link".</summary>
    public required OAuthFlow Action { get; init; }

    /// <summary>Desired username (create only). If omitted, a name will be generated from the external profile.</summary>
    [Username(true)]
    public required string? Username { get; init; }
    
    [EmailAddress(true)]
    public required string? Email { get; init; }

    /// <summary>
    /// New account password (create only).
    /// </summary>
    [Password(true)]
    public required string? Password { get; init; }
}