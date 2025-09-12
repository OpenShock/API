using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace OpenShock.API.OAuth;

public sealed record ValidatedFlowContext(string Provider, OAuthFlow Flow, string ExternalAccountId, string? ExternalAccountName, ClaimsPrincipal Principal, AuthenticationProperties Properties);