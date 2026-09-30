using OpenShock.Internal.Common.Geo;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.Geo;

namespace OpenShock.API.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNodeAsync();
    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode);

    /// <summary>
    /// Picks the closest, least loaded node. The closest region is chosen by country; when
    /// <paramref name="clientLocation"/> is known and every node in that region has coordinates, the
    /// region is further narrowed to the nodes nearest the client.
    /// </summary>
    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode, GeoPoint? clientLocation);
}