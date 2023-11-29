namespace OpenShock.Common.Models.WebSocket;

/// <summary>
/// Indicates that the websocket message received or to be sent is larger than the defined limit.
/// </summary>
public class MessageTooLongException : Exception
{
    /// <inheritdoc />
    public MessageTooLongException()
    {
    }

    /// <inheritdoc />
    public MessageTooLongException(string message) : base(message)
    {
    }
}