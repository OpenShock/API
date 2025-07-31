namespace OpenShock.API.Models.Response;

public sealed class PublicShareDevice
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IList<PublicShareShocker> Shockers { get; init; }
}