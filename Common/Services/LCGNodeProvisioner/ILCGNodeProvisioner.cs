using OpenShock.Common.Geo;
using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNodeAsync();
    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode);
}