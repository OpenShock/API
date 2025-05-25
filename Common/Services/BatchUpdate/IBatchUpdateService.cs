namespace OpenShock.Common.Services.BatchUpdate;

public interface IBatchUpdateService
{
    /// <summary>
    /// Update time of last used for a token
    /// </summary>
    /// <param name="apiTokenId"></param>
    public void UpdateApiTokenLastUsed(Guid apiTokenId);
    public void UpdateSessionLastUsed(string sessionToken, DateTimeOffset lastUsed);
}