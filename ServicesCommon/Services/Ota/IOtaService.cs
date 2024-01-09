using Semver;

namespace OpenShock.ServicesCommon.Services.Ota;

public interface IOtaService
{
    public Task Started(Guid deviceId, SemVersion version);
}