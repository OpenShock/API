using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class PasswordReset
{
    public Guid Token { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ExpiresOn { get; set; }

    public DateTimeOffset? UsedOn { get; set; }

    public virtual User User { get; set; } = null!;
}
