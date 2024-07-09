namespace OpenShock.ServicesCommon.Services.BatchUpdate;

public interface IBatchUpdateService
{
    /// <summary>
    /// Update time of last used for a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="lastUsed"></param>
    public void UpdateTokenLastUsed(Guid tokenId, DateTime? lastUsed = null);
}