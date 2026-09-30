namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// The roles that make an account privileged: automation tokens never apply to such an account, and
/// automated-account cleanup never deletes one.
/// </summary>
/// <remarks>
/// Npgsql can't translate LINQ predicates over the <c>roles</c> enum array (<c>Roles.Any(...)</c> and
/// <c>Roles.Contains(...)</c> both fail), so queries read the roles and check them with <see cref="Any"/>,
/// or pass <see cref="All"/> to SQL as a <c>role_type[]</c> parameter.
/// </remarks>
public static class PrivilegedRoles
{
    public static readonly RoleType[] All = [RoleType.Staff, RoleType.Admin, RoleType.System];

    public static bool Contains(RoleType role) => role is RoleType.Staff or RoleType.Admin or RoleType.System;

    public static bool Any(IEnumerable<RoleType> roles) => roles.Any(Contains);
}
