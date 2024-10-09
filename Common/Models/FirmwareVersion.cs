namespace OpenShock.Common.Models;

public sealed class FirmwareVersion
{
    public required Version Version { get; set; }
    public required Uri DownloadUri { get; set; }
}