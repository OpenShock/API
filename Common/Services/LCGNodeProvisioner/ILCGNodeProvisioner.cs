using OpenShock.Common.Geo;
using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNodeAsync(string environment = "Production");
    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode, string environment = "Production");
}