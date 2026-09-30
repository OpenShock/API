using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.AutomationTokens;

/// <summary>
/// The result of resolving the <c>X-OpenShock-Automation-Token</c> header against the database.
/// Set on <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> by the automation-token middleware
/// so that downstream guards (rate limiter, turnstile, etc.) can read it synchronously without
/// re-hitting the database.
/// </summary>
public sealed record ResolvedAutomationToken(Guid Id, string Name, IReadOnlyList<AutomationTokenType> Types);
