namespace ShockLink.Common.Models;

[Flags]
public enum PauseReason
{
    None            = 0,        // 0
    Shocker         = 1 << 0,   // 1
    Share           = 1 << 1,   // 2
    ShareLink       = 1 << 2    // 4
}