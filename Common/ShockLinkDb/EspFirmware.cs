using System;
using System.Collections.Generic;
using ShockLink.Common.Models;

namespace ShockLink.Common.ShockLinkDb;

public partial class EspFirmware
{
    public string Version { get; set; } = null!;
    public BranchType Branch { get; set; }
    
    public DateTime CreatedOn { get; set; }

    public string Changelog { get; set; } = null!;

    public string Commit { get; set; } = null!;
}
