using OpenShock.Common.Models;

namespace OpenShock.Common.Services.Bypass;

/// <summary>
/// The result of resolving the <c>X-OpenShock-Bypass-Token</c> header against the database.
/// Set on <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> by the bypass-token middleware
/// so that downstream guards (rate limiter, turnstile, etc.) can read it synchronously without
/// re-hitting the database.
/// </summary>
public sealed record ResolvedBypassToken(Guid Id, IReadOnlyList<BypassTokenType> Types);
