using System;
using System.Collections.Generic;

namespace ShockLink.Common.ShockLinkDb;

public partial class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool EmailActived { get; set; }

    public virtual ICollection<ShockerShare> ShockerShares { get; } = new List<ShockerShare>();

    public virtual ICollection<Shocker> Shockers { get; } = new List<Shocker>();
}
