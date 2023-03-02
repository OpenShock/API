using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class Shocker
{
    public Guid Id { get; set; }

    public int RfId { get; set; }

    public Guid Owner { get; set; }

    public string Name { get; set; } = null!;

    public virtual User OwnerNavigation { get; set; } = null!;

    public virtual ICollection<ShockerShare> ShockerShares { get; } = new List<ShockerShare>();
}
