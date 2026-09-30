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
    public void UpdateAutomationTokenUsed(Guid automationTokenId);
    public void UpdateSessionLastUsed(string sessionToken, DateTimeOffset lastUsed);
}