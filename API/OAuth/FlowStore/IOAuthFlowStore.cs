namespace OpenShock.API.OAuth.FlowStore;

public interface IOAuthFlowStore
{
    Task<string> SaveAsync(OAuthSnapshot snap, TimeSpan ttl, CancellationToken ct = default);
    Task<OAuthSnapshot?> GetAsync(string flowId, CancellationToken ct = default);
    Task DeleteAsync(string flowId, CancellationToken ct = default);
}