using OpenShock.Common.Utils;
using StackExchange.Redis;

namespace OpenShock.Common.Redis;

public static class QueueHelper
{
    public static Task ConsumeQueue(
        ChannelMessageQueue queue,
        Func<RedisValue, CancellationToken, Task> handler,
        ILogger logger,
        CancellationToken ct)
     {
        return OsTask.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var msg = await queue.ReadAsync(ct);
                if (!msg.Message.HasValue) continue;

                try
                {
                    await handler(msg.Message, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    // keep the loop alive on individual message failures
                    logger.LogError(ex, "Error while handling Redis message from {Channel}", msg.Channel);
                }
            }
        });
    }
}
