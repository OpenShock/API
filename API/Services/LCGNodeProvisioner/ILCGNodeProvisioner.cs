using OpenShock.Common.Geo;
using OpenShock.Common.Redis;

namespace OpenShock.API.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNodeAsync();
    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode);
}