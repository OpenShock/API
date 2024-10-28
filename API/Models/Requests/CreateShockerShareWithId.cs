namespace OpenShock.API.Models.Requests;

public sealed class CreateShockerShareWithId : CreateShockerShare
{
    public Guid Id { get; set; }
}