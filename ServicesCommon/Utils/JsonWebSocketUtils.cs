using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.IO;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Serialization;

namespace OpenShock.ServicesCommon.Utils;

public static class JsonWebSocketUtils
{
    private const uint MaxMessageSize = 512_000; // 512 000 bytes
    
    public static readonly RecyclableMemoryStreamManager RecyclableMemory = new();

    public static async Task<OneOf.OneOf<T?, DeserializeFailed, WebsocketClosure>> ReceiveFullMessageAsyncNonAlloc<T>(
        WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            ValueWebSocketReceiveResult result;
            await using var message = RecyclableMemory.GetStream();
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
                return SlSerializer.Deserialize<T>(message.GetBuffer().AsSpan(0, bytes));
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

    public static Task SendFullMessage<T>(T obj, WebSocket socket, CancellationToken cancelToken, int maxChunkSize = 256) =>
        SendFullMessageBytes(JsonSerializer.SerializeToUtf8Bytes(obj), socket, cancelToken);

    public static async Task SendFullMessageBytes(byte[] msg, WebSocket socket, CancellationToken cancelToken, int maxChunkSize = 256)
    {
        var doneBytes = 0;

        while (doneBytes < msg.Length)
        {
            var bytesProcessing = Math.Min(maxChunkSize, msg.Length - doneBytes);
            var buffer = msg.AsMemory(doneBytes, bytesProcessing);

            doneBytes += bytesProcessing;
            await socket.SendAsync(buffer, WebSocketMessageType.Text, doneBytes >= msg.Length, cancelToken);
        }
    }
}

/// <summary>
/// When json deserialization fails
/// </summary>
public readonly struct DeserializeFailed
{
    public required Exception Exception { get; init; }
}

/// <summary>
/// When the websocket sent a close frame
/// </summary>
public readonly struct WebsocketClosure
{
}