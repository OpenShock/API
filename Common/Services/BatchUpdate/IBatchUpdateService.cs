using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.BatchUpdate;

public interface IBatchUpdateService
{
    /// <summary>
    /// Update time of last used for a token
    /// </summary>
    /// <param name="apiTokenId"></param>
    public void UpdateApiTokenLastUsed(Guid apiTokenId);
    /// <summary>
    /// Count one accepted request for an automation token and bump its time of last use
    /// </summary>
    /// <param name="automationTokenId"></param>
    /// <param name="tokenHash">Hash of the secret presented; the use is dropped if the token was rotated since</param>
    public void UpdateAutomationTokenUsed(Guid automationTokenId, string tokenHash);
    /// <summary>
    /// Buffer a control log for the next flush, written at most once per <see cref="ShockerControlLog.Id"/>
    /// </summary>
    /// <param name="log"></param>
    public void InsertControlLog(ShockerControlLog log);
    public void UpdateSessionLastUsed(string sessionToken, DateTimeOffset lastUsed);
}