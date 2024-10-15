namespace OpenShock.Common.Authentication.Services;

public interface IClientAuthService<T>
{
    public T CurrentClient { get; set; }
}

public sealed class ClientAuthService<T> : IClientAuthService<T> where T : class
{
    public T CurrentClient { get; set; } = null!;
}