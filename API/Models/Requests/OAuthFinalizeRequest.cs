using System.ComponentModel.DataAnnotations;
using OpenShock.API.OAuth;

namespace OpenShock.API.Models.Requests;

public sealed class OAuthFinalizeRequest
{
    /// <summary>Action to perform: "create" or "link".</summary>
    public required OAuthFlow Action { get; init; }

    /// <summary>Desired username (create only). If omitted, a name will be generated from the external profile.</summary>
    public required string? Username { get; init; }
    
    public required string? Email { get; init; }

    /// <summary>
    /// New account password (create only).
    /// </summary>
    public required string? Password { get; init; }
}