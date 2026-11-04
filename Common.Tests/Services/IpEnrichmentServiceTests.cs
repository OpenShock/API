using OpenShock.Common.Services.Geo;

namespace OpenShock.Common.Tests.Services;

public class IpEnrichmentServiceTests
{
    [Test]
    [Arguments("NordVPN")]
    [Arguments("Mullvad VPN AB")]
    [Arguments("Private Internet Access, Inc.")]
    [Arguments("privateinternetaccess.com")]
    [Arguments("Hotspot Shield")]
    [Arguments("PIA")]
    public async Task MatchesVpnProvider_KnownVpnOrgs_ReturnsTrue(string asnOrg)
    {
        await Assert.That(IpEnrichmentService.MatchesVpnProvider(asnOrg)).IsTrue();
    }

    // The datacenter and hosting ASNs that used to live in the keyword list. They carry ordinary
    // traffic and must not be reported as VPNs.
    [Test]
    [Arguments("Amazon.com, Inc.")]
    [Arguments("Google LLC")]
    [Arguments("Microsoft Corporation")]
    [Arguments("Akamai Technologies, Inc.")]
    [Arguments("OVH SAS")]
    [Arguments("Hetzner Online GmbH")]
    [Arguments("DigitalOcean, LLC")]
    public async Task MatchesVpnProvider_HostingOrgs_ReturnsFalse(string asnOrg)
    {
        await Assert.That(IpEnrichmentService.MatchesVpnProvider(asnOrg)).IsFalse();
    }

    // "pia" is a real vendor abbreviation and also a substring of ordinary words. Whole-token
    // matching is what keeps the second case from being reported as a VPN.
    [Test]
    [Arguments("Olympia Networks")]
    [Arguments("Compia Telecom")]
    [Arguments("Utopia Broadband")]
    public async Task MatchesVpnProvider_SubstringLookalikes_ReturnsFalse(string asnOrg)
    {
        await Assert.That(IpEnrichmentService.MatchesVpnProvider(asnOrg)).IsFalse();
    }

    [Test]
    public async Task MatchesVpnProvider_EmptyOrg_ReturnsFalse()
    {
        await Assert.That(IpEnrichmentService.MatchesVpnProvider(string.Empty)).IsFalse();
    }
}
