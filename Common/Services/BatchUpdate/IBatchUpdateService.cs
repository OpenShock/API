namespace OpenShock.ServicesCommon.Services.BatchUpdate;

public interface IBatchUpdateService
{
    /// <summary>
    /// Update time of last used for a token
    /// </summary>
    /// <param name="tokenId"></param>
    public void UpdateTokenLastUsed(Guid tokenId);
}