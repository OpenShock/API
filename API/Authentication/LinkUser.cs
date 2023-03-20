using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Authentication;

public class LinkUser
{
    public User DbUser { get; set; }

    public Uri GetImageLink() => GetImageLink(ImageVariant.x512);
    public Uri GetImageLink(ImageVariant variant) => ImagesApi.GetImage(DbUser.Image, variant);
}