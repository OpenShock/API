using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using Microsoft.IO;
using OpenShock.Common.Serialization;

namespace OpenShock.API.Utils;

public static class WebSocketUtils
{

    private static readonly RecyclableMemoryStreamManager RecyclableMemory = new();

    public static async Task<(ValueWebSocketReceiveResult, T?)> ReceiveFullMessageAsyncNonAlloc<T>(
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
                    return (result, default);
                }

                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            return (result, SlSerializer.Deserialize<T>(message.GetBuffer().AsSpan(0, bytes)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static Task SendFullMessage<T>(T obj, WebSocket socket, CancellationToken cancelToken) =>
        SendFullMessage(System.Text.Json.JsonSerializer.Serialize(obj), socket, cancelToken);

    public static Task SendFullMessage(string json, WebSocket socket, CancellationToken cancelToken) =>
        SendFullMessageBytes(Encoding.UTF8.GetBytes(json), socket, cancelToken);

    public static async Task SendFullMessageBytes(byte[] msg, WebSocket socket, CancellationToken cancelToken)
    {
        var doneBytes = 0;

        while (doneBytes < msg.Length)
        {
            var bytesProcessing = Math.Min(1024, msg.Length - doneBytes);
            var buffer = msg.AsMemory(doneBytes, bytesProcessing);

            doneBytes += bytesProcessing;
            await socket.SendAsync(buffer, WebSocketMessageType.Text, doneBytes >= msg.Length, cancelToken);
        }
    }
}