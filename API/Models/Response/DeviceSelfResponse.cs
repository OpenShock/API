namespace OpenShock.API.Models.Response;

public sealed class DeviceSelfResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required MinimalShocker[] Shockers { get; init; }
}