using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class ShareRequest
{
    public Guid Id { get; set; }

    public Guid Owner { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public Guid? User { get; set; }

    public virtual User OwnerNavigation { get; set; } = null!;

    public virtual User? UserNavigation { get; set; }
}
