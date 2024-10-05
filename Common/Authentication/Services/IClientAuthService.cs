namespace OpenShock.Common.Authentication.Services;

public interface IClientAuthService<T>
{
    public T CurrentClient { get; set; }
}