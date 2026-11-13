using System.ComponentModel.DataAnnotations;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.API.Models.Requests;

public sealed class CreateShareRequest
{
    [MaxLength(HardLimits.CreateShareRequestMaxShockers)]
    public required ShockerPermLimitPairWithId[] Shockers { get; init; }
    public Guid? User { get; init; } = null;
}
