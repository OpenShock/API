namespace OpenShock.Common.OpenShockDb;

public partial class UserNameChange
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string OldName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
