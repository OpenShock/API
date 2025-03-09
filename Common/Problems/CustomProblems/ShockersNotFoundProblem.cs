using System.Net;

namespace OpenShock.Common.Problems.CustomProblems;

public sealed class ShockersNotFoundProblem(
    string type,
    string title,
    IReadOnlyList<Guid> missingShockers,
    HttpStatusCode status = HttpStatusCode.BadRequest,
    string? detail = null)
    : OpenShockProblem(type, title, status, detail)
{
    public IReadOnlyList<Guid> MissingShockers { get; set; } = missingShockers;
}