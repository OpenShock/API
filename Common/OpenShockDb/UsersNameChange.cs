using System;
using System.Collections.Generic;

namespace OpenShock.Common.OpenShockDb;

public partial class UsersNameChange
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string OldName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public virtual User User { get; set; } = null!;
}
