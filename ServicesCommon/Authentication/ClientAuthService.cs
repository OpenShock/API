namespace OpenShock.ServicesCommon.Authentication;

public class ClientAuthService<T> : IClientAuthService<T>
{
    public T CurrentClient { get; set; } = default!;
}