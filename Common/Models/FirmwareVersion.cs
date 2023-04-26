namespace ShockLink.Common.Models;

public class FirmwareVersion
{
    public required Version Version { get; set; }
    public required Uri DownloadUri { get; set; }
}