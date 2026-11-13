using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class GravatarUtilsTests
{
    [Test]
    public async Task GuestImageUrl_IsGravatarUrl()
    {
        await Assert.That(GravatarUtils.GuestImageUrl.Host).IsEqualTo("www.gravatar.com");
    }

    [Test]
    public async Task GuestImageUrl_UsesZeroHash()
    {
        await Assert.That(GravatarUtils.GuestImageUrl.AbsolutePath).IsEqualTo("/avatar/0");
    }

    [Test]
    public async Task GetUserImageUrl_IsGravatarUrl()
    {
        var url = GravatarUtils.GetUserImageUrl("test@example.com");
        await Assert.That(url.Host).IsEqualTo("www.gravatar.com");
    }

    [Test]
    public async Task GetUserImageUrl_ContainsEmailHash()
    {
        var email = "test@example.com";
        var expectedHash = HashingUtils.HashSha256(email);
        var url = GravatarUtils.GetUserImageUrl(email);
        await Assert.That(url.AbsolutePath).IsEqualTo($"/avatar/{expectedHash}");
    }

    [Test]
    public async Task GetUserImageUrl_ContainsDefaultImageParam()
    {
        var url = GravatarUtils.GetUserImageUrl("test@example.com");
        await Assert.That(url.Query).Contains("d=");
    }

    [Test]
    public async Task GetUserImageUrl_DifferentEmails_ProduceDifferentUrls()
    {
        var url1 = GravatarUtils.GetUserImageUrl("a@example.com");
        var url2 = GravatarUtils.GetUserImageUrl("b@example.com");
        await Assert.That(url1).IsNotEqualTo(url2);
    }
}
