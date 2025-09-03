namespace OpenShock.API.OAuth.FlowStore;

public interface IOAuthFlowStore
{
    Task<string> SaveAsync(string provider, string externalId, string? email, string? displayName, DateTimeOffset issuedUtc);
    Task<OAuthSnapshot?> GetAsync(string flowId);
    Task DeleteAsync(string flowId);
}