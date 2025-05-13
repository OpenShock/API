using OpenShock.Common.Models;

namespace OpenShock.API.Utils;

public static class PublicShareUtils
{
    public static PauseReason GetPausedReason(bool publicShareLevel, bool shockerLevel) => publicShareLevel switch
    {
        true when shockerLevel => PauseReason.Shocker | PauseReason.ShareLink,
        true => PauseReason.ShareLink,
        _ => shockerLevel ? PauseReason.Shocker : PauseReason.None
    };
}