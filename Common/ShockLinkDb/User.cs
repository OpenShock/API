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

    public Guid Image { get; set; }

    public virtual ICollection<ApiToken> ApiTokens { get; set; } = new List<ApiToken>();

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual CfImage ImageNavigation { get; set; } = null!;

    public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();

    public virtual ICollection<ShockerControlLog> ShockerControlLogs { get; set; } = new List<ShockerControlLog>();

    public virtual ICollection<ShockerShare> ShockerShares { get; set; } = new List<ShockerShare>();
}
