namespace OpenShock.Common.Models;

public sealed class FirmwareVersion
{
    public required Version Version { get; init; }
    public required Uri DownloadUri { get; init; }
}
