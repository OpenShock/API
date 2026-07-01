using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

public static class NpgsqlEnumExtensions
{
    private readonly record struct PgEnumInfo(
        Action<NpgsqlDbContextOptionsBuilder> Map,
        Action<ModelBuilder> Register);

    private static PgEnumInfo BuildInfo<TEnum>() where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        var attr = type.GetCustomAttribute<PgEnumAttribute>()
            ?? throw new InvalidOperationException($"{type.Name} is missing [PgEnum]");

        var pgTypeName = attr.Name;

        var members = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetCustomAttribute<PgNameAttribute>()?.PgName)
            .Where(n => n is not null)
            .Cast<string>()
            .ToArray();

        return new PgEnumInfo(
            Map: b => b.MapEnum<TEnum>(pgTypeName),
            Register: m => m.HasPostgresEnum(attr.Schema, pgTypeName, members));
    }

    private static readonly PgEnumInfo[] Enums =
    [
        BuildInfo<ControlType>(),
        BuildInfo<ControlLimitMode>(),
        BuildInfo<OtaUpdateStatus>(),
        BuildInfo<PasswordHashingAlgorithm>(),
        BuildInfo<PermissionType>(),
        BuildInfo<RoleType>(),
        BuildInfo<ShockerModelType>(),
        BuildInfo<MatchTypeEnum>(),
        BuildInfo<ConfigurationValueType>(),
        BuildInfo<EmailType>(),
        BuildInfo<EmailStatus>(),
    ];

    public static NpgsqlDbContextOptionsBuilder MapPgEnums(this NpgsqlDbContextOptionsBuilder builder)
    {
        foreach (var info in Enums)
            info.Map(builder);
        return builder;
    }

    public static ModelBuilder RegisterPgEnums(this ModelBuilder modelBuilder)
    {
        foreach (var info in Enums)
            info.Register(modelBuilder);
        return modelBuilder;
    }
}
