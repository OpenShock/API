using System.Security.Claims;
using NpgsqlTypes;
// ReSharper disable InconsistentNaming

namespace ShockLink.Common.Models;

public enum PermissionType
{
    [PgName("shockers.use")] Shockers_Use
}

public static class PermissionTypeBindings
{
    public static readonly IReadOnlyCollection<string> DatabaseNames = InitDatabaseNames();
    public static readonly IReadOnlyCollection<Claim> RoleClaimNames = ConvertToRoleClaims(DatabaseNames).ToArray();
    public static readonly List<PermissionType> AllPermissionTypes = new(Enum.GetValues<PermissionType>());

    public static readonly IReadOnlyDictionary<PermissionType, Claim> TypeToName =
        new Dictionary<PermissionType, Claim>
        {
            { PermissionType.Shockers_Use, new Claim(ClaimTypes.Role, "shockers.use") }
        };

    private static IEnumerable<Claim> ConvertToRoleClaims(IReadOnlyCollection<string> input) =>
        input.Select(s => new Claim(ClaimTypes.Role, s));


    private static IReadOnlyCollection<string> InitDatabaseNames()
    {
        var fields = typeof(PermissionType).GetFields();
        var names = new List<string>();
        foreach (var fieldInfo in fields)
            names.AddRange(fieldInfo.GetCustomAttributes(typeof(PgNameAttribute), false)
                .Select(attribute => ((PgNameAttribute)attribute).PgName));
        return names;
    }
}