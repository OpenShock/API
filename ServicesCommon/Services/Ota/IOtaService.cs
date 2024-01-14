using Semver;

namespace OpenShock.ServicesCommon.Services.Ota;

public interface IOtaService
{
    public Task Started(Guid deviceId, int updateId, SemVersion version);
    public Task Progress(Guid deviceId, int updateId);
    public Task Error(Guid deviceId, int updateId);
    public Task Success(Guid deviceId, int updateId);
}