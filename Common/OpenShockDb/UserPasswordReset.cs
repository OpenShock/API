namespace OpenShock.Common.OpenShockDb;

public partial class UserPasswordReset
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public string SecretHash { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
