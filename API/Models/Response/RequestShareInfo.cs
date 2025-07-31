using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;

namespace OpenShock.API.Models.Response;

public sealed class RequestShareInfo : ShockerPermLimitPairWithId
{
    public required BasicUserInfo? SharedWith { get; init; }
    public required DateTime CreatedOn { get; init; }
}