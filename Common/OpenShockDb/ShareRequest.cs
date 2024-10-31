using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequest
{
    public Guid Id { get; set; }

    public Guid Owner { get; set; }

    public DateTime CreatedOn { get; set; }

    public Guid? User { get; set; }

    public virtual User OwnerNavigation { get; set; } = null!;

    public virtual ICollection<ShareRequestsShocker> ShareRequestsShockers { get; set; } = new List<ShareRequestsShocker>();

    public virtual User? UserNavigation { get; set; }
}
