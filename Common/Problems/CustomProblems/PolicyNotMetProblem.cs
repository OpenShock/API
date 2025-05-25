using System.Net;

namespace OpenShock.Common.Problems.CustomProblems;

public class PolicyNotMetProblem : OpenShockProblem
{
    public PolicyNotMetProblem(IEnumerable<string> failedRequirements) : base(
        "Authorization.Policy.NotMet",
        "One or multiple policies were not met", HttpStatusCode.Forbidden, string.Empty)
    {
        FailedRequirements = failedRequirements;
    }
    
    public IEnumerable<string> FailedRequirements { get; set; }
}