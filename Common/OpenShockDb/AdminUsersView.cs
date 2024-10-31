using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public class AdminUsersView
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHashType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool EmailActived { get; set; }

    public RankType Rank { get; set; }

    public int ApiTokenCount { get; set; }

    public int PasswordResetCount { get; set; }

    public int ShockerShareCount { get; set; }

    public int ShockerShareLinkCount { get; set; }

    public int EmailChangeRequestCount { get; set; }

    public int NameChangeRequestCount { get; set; }

    public int UserActivationCount { get; set; }

    public int DeviceCount { get; set; }

    public int ShockerCount { get; set; }

    public int ShockerControlLogCount { get; set; }
}
