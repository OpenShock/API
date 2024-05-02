using System.Reflection;
using System.Text.Json.Serialization;
using NpgsqlTypes;
using OpenShock.Common.JsonSerialization;

// ReSharper disable InconsistentNaming

namespace OpenShock.Common.Models;

[JsonConverter(typeof(PermissionTypeConverter))]
public enum PermissionType
{
    [PgName("shockers.use")] Shockers_Use,
    [PgName("shockers.edit")] Shockers_Edit
}

public static class PermissionTypeBindings
{
    public static readonly IReadOnlyDictionary<PermissionType, string> PermissionTypeToName = Init();

    public static readonly IReadOnlyDictionary<string, PermissionType> NameToPermissionType =
        ReverseDic(PermissionTypeToName);

    public static IReadOnlyDictionary<PermissionType, string> Init()
    {
        var enumValues = Enum.GetValues<PermissionType>();
        var fields = typeof(PermissionType).GetFields();

        return enumValues.ToDictionary(x => x,
            x => fields.First(y => y.Name == x.ToString()).GetCustomAttribute<PgNameAttribute>()!.PgName);
    }

    public static IReadOnlyDictionary<T0, T1> ReverseDic<T1, T0>(this IReadOnlyDictionary<T1, T0> dic)
        where T0 : notnull where T1 : notnull =>
        dic.ToDictionary(x => x.Value, x => x.Key);
}