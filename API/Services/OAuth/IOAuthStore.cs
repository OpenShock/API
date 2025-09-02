namespace OpenShock.API.Services.OAuth;

public interface IOAuthStateStore
{
    void Save(HttpContext http, string provider, string state, string? returnTo);
    (string State, string? ReturnTo)? ReadAndClear(HttpContext http, string provider);
}