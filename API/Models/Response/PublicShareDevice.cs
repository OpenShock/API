namespace OpenShock.API.Models.Response;

public sealed class PublicShareDevice
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required IList<PublicShareShocker> Shockers { get; set; }
}