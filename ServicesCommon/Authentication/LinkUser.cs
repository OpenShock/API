using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.ServicesCommon.Authentication;

public class LinkUser
{
    public required User DbUser { get; set; }

    public Uri GetImageLink() => GravatarUtils.GetImageUrl(DbUser.Email);
}