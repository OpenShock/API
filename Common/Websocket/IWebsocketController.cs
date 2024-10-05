using Microsoft.AspNetCore.Mvc;

namespace OpenShock.Common.Websocket;

/// <summary>
/// Interface description for a websocket controller with any type
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IWebsocketController<in T>
{
    /// <summary>
    /// Main identifier for the websocket connection, this might be a user or device id
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Queue a message to be sent to the client, usually instant
    /// </summary>
    /// <param name="data"></param>
    /// <returns>ValueTask</returns>
    [NonAction]
    public ValueTask QueueMessage(T data);
}