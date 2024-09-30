using OpenShock.Common.Redis;
using OpenShock.Common.Utils;

namespace OpenShock.ServicesCommon.Services.LCGNodeProvisioner;

public interface ILCGNodeProvisioner
{
    public Task<LcgNode?> GetOptimalNode(string environment = "Production");
    public Task<LcgNode?> GetOptimalNode(CountryCodeMapper.Alpha2CountryCode countryCode, string environment = "Production");
}