namespace OpenShock.API.Models.Response;

public sealed class OAuthFinalizeResponse
{
    /// <summary>"ok" on success; otherwise not returned (problem details emitted).</summary>
    public string Status { get; init; } = "ok";

    /// <summary>The provider key that was processed.</summary>
    public required string Provider { get; init; }

    /// <summary>The external account id that was linked.</summary>
    public required string ExternalId { get; init; }

    /// <summary>When action=create, the username of the newly created account.</summary>
    public string? Username { get; init; }
}