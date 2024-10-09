using OpenShock.Common.Geo;
using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNode(string environment = "Production");
    public Task<LcgNode?> GetOptimalNode(Alpha2CountryCode countryCode, string environment = "Production");
}