using TUnit.Core;

// Fail individual tests after 30 seconds to prevent thread pool starvation
// from causing long hangs (BCrypt is synchronous and CPU-bound).
[assembly: Timeout(30_000)]
