namespace OpenShock.Common.Authentication.Services;

public interface ITokenReferenceService<T>
{
    public T? Token { get; set; }
}