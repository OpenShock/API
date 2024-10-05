using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.ServicesCommon.Authentication;

public sealed class LinkUser
{
    public required User DbUser { get; set; }

    public Uri GetImageLink() => GravatarUtils.GetImageUrl(DbUser.Email);
}