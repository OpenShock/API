using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class IpAddressUtilsTests
{
    [Test]
    public async Task ParseStoredOrThrow_Null_ReturnsNull()
    {
        await Assert.That(IpAddressUtils.ParseStoredOrThrow(null, NullLogger.Instance)).IsNull();
    }

    [Test]
    [Arguments("127.0.0.1")]
    [Arguments("::1")]
    public async Task ParseStoredOrThrow_Valid_ReturnsAddress(string stored)
    {
        await Assert.That(IpAddressUtils.ParseStoredOrThrow(stored, NullLogger.Instance)).IsEqualTo(IPAddress.Parse(stored));
    }

    [Test]
    [Arguments("")]
    [Arguments("not an ip")]
    public async Task ParseStoredOrThrow_Malformed_Throws(string stored)
    {
        await Assert.That(() => IpAddressUtils.ParseStoredOrThrow(stored, NullLogger.Instance)).Throws<FormatException>();
    }

    [Test]
    public async Task ParseStoredOrNull_Null_ReturnsNull()
    {
        await Assert.That(IpAddressUtils.ParseStoredOrNull(null, NullLogger.Instance)).IsNull();
    }

    [Test]
    [Arguments("127.0.0.1")]
    [Arguments("::1")]
    public async Task ParseStoredOrNull_Valid_ReturnsAddress(string stored)
    {
        await Assert.That(IpAddressUtils.ParseStoredOrNull(stored, NullLogger.Instance)).IsEqualTo(IPAddress.Parse(stored));
    }

    [Test]
    [Arguments("")]
    [Arguments("not an ip")]
    public async Task ParseStoredOrNull_Malformed_ReturnsNull(string stored)
    {
        await Assert.That(IpAddressUtils.ParseStoredOrNull(stored, NullLogger.Instance)).IsNull();
    }
}
