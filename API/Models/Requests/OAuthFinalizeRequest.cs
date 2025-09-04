using System.ComponentModel.DataAnnotations;

namespace OpenShock.API.Models.Requests;

public sealed class OAuthFinalizeRequest
{
    /// <summary>Action to perform: "create" or "link".</summary>
    [Required]
    public required string Action { get; init; }

    /// <summary>Desired username (create only). If omitted, a name will be generated from the external profile.</summary>
    public string? Username { get; init; }
    
    public string? Email { get; init; }

    /// <summary>
    /// New account password (create only).
    /// </summary>
    public string? Password { get; init; }
}