namespace ShockLink.API.Authentication;

public interface IClientAuthService<T>
{
    public T CurrentClient { get; set; }
}