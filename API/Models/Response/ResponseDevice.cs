namespace OpenShock.API.Models.Response;

public class ResponseDevice
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedOn { get; init; }
}