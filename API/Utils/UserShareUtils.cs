using OpenShock.Common.Models;

namespace OpenShock.API.Utils;

public static class UserShareUtils
{
    public static PauseReason GetPausedReason(bool userShareLevel, bool shockerLevel) => userShareLevel switch
    {
        true when shockerLevel => PauseReason.Shocker | PauseReason.Share,
        true => PauseReason.Share,
        _ => shockerLevel ? PauseReason.Shocker : PauseReason.None
    };
}