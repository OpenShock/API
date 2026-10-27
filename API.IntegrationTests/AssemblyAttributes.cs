using TUnit.Core;
using TUnit.Core.Interfaces;

// Allow up to 5 minutes per test — integration tests can be slow in CI when Docker images
// are cold-pulled and EF migrations run for the first time. The execution timer in TUnit
// may include class-data-source initialization time for the first test that uses the factory.
[assembly: Timeout(5 * 60_000)]

// Limit parallel test execution to avoid thread pool starvation on CI runners.
// BCrypt password hashing in login/signup endpoints is synchronous and CPU-bound;
// too many concurrent tests exhaust the thread pool, causing request timeouts.
[assembly: ParallelLimiter<OpenShock.API.IntegrationTests.CiSafeParallelLimit>]

namespace OpenShock.API.IntegrationTests;

public record CiSafeParallelLimit : IParallelLimit
{
    public int Limit => Math.Max(Environment.ProcessorCount * 2, 8);
}

public static class ThreadPoolSetup
{
    // Running tests block on the shared host's boot, which connects to Redis synchronously and needs a free thread
    [Before(TestSession)]
    public static void RaiseMinThreads()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
        ThreadPool.SetMinThreads(Math.Max(workerThreads, new CiSafeParallelLimit().Limit * 2), completionPortThreads);
    }
}
