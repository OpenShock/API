using System.Buffers;
using System.Net.WebSockets;
using FlatSharp;
using OneOf;
using OpenShock.Common.Utils;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Flatbuffers websocket utilities
/// </summary>
public static class FlatbufferWebSocketUtils
{
    private const uint MaxMessageSize = 512_000; // 512 000 bytes
    
    /// <summary>
    /// Receive a websocket message with the given FlatBuffer type
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="serializer"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="MessageTooLongException"></exception>
    public static async Task<OneOf<T?, DeserializeFailed, WebsocketClosure>> ReceiveFullMessageAsyncNonAlloc<T>(
        WebSocket socket, ISerializer<T> serializer, CancellationToken cancellationToken)
        where T : class, IFlatBufferSerializable
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            ValueWebSocketReceiveResult result;
            await using var message = JsonWebSocketUtils.RecyclableMemory.GetStream();
            var bytes = 0;
            do
            {
                result = await socket.ReceiveAsync(new Memory<byte>(buffer), cancellationToken);
                bytes += result.Count;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closure during message read",
                        cancellationToken);
                    return new WebsocketClosure();
                }

                if (buffer.Length + result.Count > MaxMessageSize) throw new MessageTooLongException();

                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            try
            {
                return serializer.Parse(message.GetBuffer().AsMemory(0, bytes));
            }
            catch (Exception e)
            {
                return new DeserializeFailed { Exception = e };
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Send a FlatBuffer type over websocket
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="serializer"></param>
    /// <param name="socket"></param>
    /// <param name="cancelToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="MessageTooLongException"></exception>
    public static Task SendFullMessage<T>(T obj, ISerializer<T> serializer, WebSocket socket,
        CancellationToken cancelToken) where T : class, IFlatBufferSerializable
    {
        var maxSize = serializer.GetMaxSize(obj);
        if (maxSize > MaxMessageSize) throw new MessageTooLongException();

        var buffer = ArrayPool<byte>.Shared.Rent(maxSize);

        try
        {
            var bytesWritten = serializer.Write(buffer, obj);
            return SendFullMessageBytes(buffer.AsMemory(0, bytesWritten), socket, cancelToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task SendFullMessageBytes(ReadOnlyMemory<byte> msg, WebSocket socket,
        CancellationToken cancelToken)
    {
        var doneBytes = 0;

        while (doneBytes < msg.Length)
        {
            var bytesProcessing = Math.Min(1024, msg.Length - doneBytes);
            var buffer = msg.Slice(doneBytes, bytesProcessing);

            doneBytes += bytesProcessing;
            await socket.SendAsync(buffer, WebSocketMessageType.Binary, doneBytes >= msg.Length, cancelToken);
        }
    }
}

/// <summary>
/// When flatbuffers deserialization fails
/// </summary>
public readonly record struct DeserializeFailed(Exception Exception);

/// <summary>
/// When the websocket sent a close frame
/// </summary>
public readonly struct WebsocketClosure;