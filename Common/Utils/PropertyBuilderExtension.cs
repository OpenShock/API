using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OpenShock.Common.Utils;

public static class PropertyBuilderExtension
{
    public static PropertyBuilder<TProperty> VarCharWithLength<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, int length)
    {
        return propertyBuilder.HasColumnType($"character varying({length})").HasMaxLength(length);
    }
}