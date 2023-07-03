using ShockLink.API.Utils;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Authentication;

public class LinkUser
{
    public required User DbUser { get; set; }

    public Uri GetImageLink() => ImagesApi.GetImageRoot(DbUser.Image);
}