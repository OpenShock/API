using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class ShockerShare
{
    public Guid ShockerId { get; set; }

    public Guid SharedWith { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual User SharedWithNavigation { get; set; } = null!;

    public virtual Shocker Shocker { get; set; } = null!;
}
