namespace ShockLink.Common.Models.WebSocket;

public interface IBaseResponse<T> where T : Enum
{
    public T ResponseType { get; set; }
    public object? Data { get; set; }
}