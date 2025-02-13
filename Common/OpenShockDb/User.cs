using System;
using System.Collections.Generic;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public partial class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool EmailActivated { get; set; }

    public List<RoleType> Roles { get; set; } = null!;

    public virtual ICollection<ApiToken> ApiTokens { get; set; } = new List<ApiToken>();

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();

    public virtual ICollection<ShareRequest> ShareRequestOwnerNavigations { get; set; } = new List<ShareRequest>();

    public virtual ICollection<ShareRequest> ShareRequestUserNavigations { get; set; } = new List<ShareRequest>();

    public virtual ICollection<ShockerControlLog> ShockerControlLogs { get; set; } = new List<ShockerControlLog>();

    public virtual ICollection<ShockerShare> ShockerShares { get; set; } = new List<ShockerShare>();

    public virtual ICollection<ShockerSharesLink> ShockerSharesLinks { get; set; } = new List<ShockerSharesLink>();

    public virtual ICollection<UsersActivation> UsersActivations { get; set; } = new List<UsersActivation>();

    public virtual ICollection<UsersEmailChange> UsersEmailChanges { get; set; } = new List<UsersEmailChange>();

    public virtual ICollection<UsersNameChange> UsersNameChanges { get; set; } = new List<UsersNameChange>();
}
