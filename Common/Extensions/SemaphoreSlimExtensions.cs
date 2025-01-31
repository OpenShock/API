namespace OpenShock.Common.Extensions;

public static class SemaphoreSlimExtensions
{
    public static async Task<IDisposable> LockAsyncScoped(this SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        return new Releaser(semaphore);
    }
    public static async Task<IDisposable> LockAsyncScoped(this SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}