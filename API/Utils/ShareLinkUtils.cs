using OpenShock.Common.Models;

namespace OpenShock.API.Utils;

public static class ShareLinkUtils
{
    public static PauseReason GetPausedReason(bool shareLinkLevel, bool shockerLevel) => shareLinkLevel switch
    {
        true when shockerLevel => PauseReason.Shocker | PauseReason.ShareLink,
        true => PauseReason.ShareLink,
        _ => shockerLevel ? PauseReason.Shocker : PauseReason.None
    };
}