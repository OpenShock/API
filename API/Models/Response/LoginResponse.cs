using System.Text.Json.Serialization;

namespace OpenShock.API.Models.Response;

public class LoginResponse
{
    public required string SessionToken { get; set; }
    public required DateTime ValidUntil { get; set; }
}