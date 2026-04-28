using OpenShock.Common.Models;

namespace OpenShock.Common.Utils;

public static class PermissionUtils
{
    public static bool IsAllowed(ControlType type, bool isLive, SharePermsAndLimits? perms)
    {
        if (perms is null) return true;
        if (isLive && !perms.Live) return false;
        return type switch
        {
            ControlType.Shock => perms.Shock,
            ControlType.Vibrate => perms.Vibrate,
            ControlType.Sound => perms.Sound,
            ControlType.Stop => perms.Shock || perms.Vibrate || perms.Sound,
            _ => false
        };
    }
}