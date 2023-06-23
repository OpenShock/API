using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class PasswordReset
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ExpiresOn { get; set; }

    public DateTimeOffset? UsedOn { get; set; }

    public string Secret { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
