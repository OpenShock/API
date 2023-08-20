namespace ShockLink.Common.Models;

[Flags]
public enum PauseReason
{
    None            = 0,
    Shocker         = 1 << 0,
    Share           = 1 << 1,
    ShareLink       = 1 << 2
}