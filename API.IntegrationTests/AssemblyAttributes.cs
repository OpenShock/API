using TUnit.Core;
using TUnit.Core.Interfaces;

// Fail individual tests after 60 seconds to catch hangs without being too aggressive.
[assembly: Timeout(60_000)]

// Limit parallel test execution to avoid thread pool starvation on CI runners.
// BCrypt password hashing in login/signup endpoints is synchronous and CPU-bound;
// too many concurrent tests exhaust the thread pool, causing request timeouts.
[assembly: ParallelLimiter<OpenShock.API.IntegrationTests.CiSafeParallelLimit>]

namespace OpenShock.API.IntegrationTests;

public record CiSafeParallelLimit : IParallelLimit
{
    public int Limit => Math.Max(Environment.ProcessorCount * 2, 8);
}
