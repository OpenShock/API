namespace OpenShock.ServicesCommon.Authentication;

public interface IClientAuthService<T>
{
    public T CurrentClient { get; set; }
}