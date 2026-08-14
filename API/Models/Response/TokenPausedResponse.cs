namespace OpenShock.API.Models.Response;

public sealed class TokenPausedResponse
{
    /// <summary>
    /// The now-set paused state of the token.
    /// </summary>
    public required bool Paused { get; init; }
}
