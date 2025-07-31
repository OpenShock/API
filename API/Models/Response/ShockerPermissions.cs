namespace OpenShock.API.Models.Response;

public sealed class ShockerPermissions
{
    public required bool Vibrate { get; init; }
    public required bool Sound { get; init; }
    public required bool Shock { get; init; }
    public bool Live { get; init; } = false;
}