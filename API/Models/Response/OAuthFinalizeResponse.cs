namespace OpenShock.API.Models.Response;

public sealed class OAuthFinalizeResponse
{
    /// <summary>The provider key that was processed.</summary>
    public required string Provider { get; init; }

    /// <summary>The external account id that was linked.</summary>
    public required string ExternalId { get; init; }

    /// <summary>When action=create, the username of the newly created account.</summary>
    public required string? Username { get; init; }
}