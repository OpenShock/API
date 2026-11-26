using System.Net;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.Problems.CustomProblems;

public sealed class InvalidEmailProviderDomainsProblem : OpenShockProblem
{
    /// <summary>
    /// How many invalid entries are echoed back. A paste of tens of thousands of lines shouldn't produce an
    /// error response just as large; the first few are enough to find the problem.
    /// </summary>
    public const int MaxListed = 100;

    public InvalidEmailProviderDomainsProblem(IReadOnlyCollection<string> invalidEntries) : base(
        "Admin.EmailProviderBlacklist.InvalidDomains",
        "One or more entries are not valid domains, nothing was added", HttpStatusCode.BadRequest)
    {
        InvalidCount = invalidEntries.Count;
        InvalidEntries = invalidEntries.Take(MaxListed).ToArray();
    }

    public int InvalidCount { get; init; }

    public string[] InvalidEntries { get; init; }
}
