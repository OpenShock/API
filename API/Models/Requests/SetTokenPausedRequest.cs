namespace OpenShock.API.Models.Requests;

public sealed class SetTokenPausedRequest
{
    /// <summary>
    /// When true, the token is paused and may not send shocker control messages.
    /// </summary>
    public required bool Paused { get; set; }
}
