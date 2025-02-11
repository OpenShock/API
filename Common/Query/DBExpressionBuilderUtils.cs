using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenShock.Common.Query;

public static class DBExpressionBuilderUtils
{
    private static readonly MethodInfo EfFunctionsCollateMethodInfo = typeof(RelationalDbFunctionsExtensions).GetMethod("Collate")?.MakeGenericMethod(typeof(string)) ?? throw new MissingMethodException("EF.Functions", "Collate(string,string)");
    private static readonly MethodInfo EfFunctionsLikeMethodInfo = typeof(NpgsqlDbFunctionsExtensions).GetMethod("Like", [typeof(DbFunctions), typeof(string), typeof(string)]) ?? throw new MissingMethodException("EF.Functions", "Like(string,string)");
    private static readonly MethodInfo EfFunctionsILikeMethodInfo = typeof(NpgsqlDbFunctionsExtensions).GetMethod("ILike", [typeof(DbFunctions), typeof(string), typeof(string)]) ?? throw new MissingMethodException("EF.Functions", "ILike(string,string)");
    private static readonly MethodInfo StringEqualsMethodInfo = typeof(string).GetMethod("Equals", [typeof(string)]) ?? throw new MissingMethodException("string", "Equals(string,StringComparison)");
    private static readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethod("StartsWith", [typeof(string)]) ?? throw new MissingMethodException("string", "StartsWith(string)");
    private static readonly MethodInfo StringEndsWithMethodInfo = typeof(string).GetMethod("EndsWith", [typeof(string)]) ?? throw new MissingMethodException("string", "EndsWith(string)");
    private static readonly MethodInfo StringContainsMethodInfo = typeof(string).GetMethod("Contains", [typeof(string)]) ?? throw new MissingMethodException("string","Contains(string)");

    /// <summary>
    /// To not let whoever's requesting to explore hidden data structures, we return same exception for all errors here
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propOrFieldName"></param>
    /// <returns></returns>
    /// <exception cref="MissingMemberException"></exception>
    public static (MemberInfo, Type) GetPropertyOrField(Type type, string propOrFieldName)
    {
        var memberInfo = type.GetMember(propOrFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.IgnoreCase).SingleOrDefault();
        if (memberInfo == null)
            throw new DBExpressionBuilderException($"'{propOrFieldName}' is not a valid property of type {type.Name}");

        var isIgnored = memberInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any();
        if (isIgnored)
            throw new DBExpressionBuilderException($"'{propOrFieldName}' is not a valid property of type {type.Name}");

        var memberType = memberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new DBExpressionBuilderException($"'{propOrFieldName}' is not a valid property of type {type.Name}")
        };

        return (memberInfo, memberType);
    }

    private static ConstantExpression GetConstant(Type type, string value)
    {
        if (type.IsEnum)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"'{type.Name}' cannot be null or empty");
            }
            
            object enumValue;
            try
            {
                enumValue = Enum.Parse(type, value, ignoreCase: true);
            }
            catch (ArgumentException e)
            {
                throw new FormatException($"'{value}' is not a valid value for type {type.Name}", e);
            }
            return Expression.Constant(enumValue, type);
        }

        // ReSharper disable BuiltInTypeReferenceStyleForMemberAccess
        return Expression.Constant(Type.GetTypeCode(type) switch
        {
            TypeCode.Empty => throw new NotImplementedException(),
            TypeCode.Object => HandleObject(type, value),
            TypeCode.DBNull => throw new NotImplementedException(),
            TypeCode.Boolean => Boolean.Parse(value),
            TypeCode.Char => Char.Parse(value),
            TypeCode.SByte => SByte.Parse(value),
            TypeCode.Byte => Byte.Parse(value),
            TypeCode.Int16 => Int16.Parse(value),
            TypeCode.UInt16 => UInt16.Parse(value),
            TypeCode.Int32 => Int32.Parse(value),
            TypeCode.UInt32 => UInt32.Parse(value),
            TypeCode.Int64 => Int64.Parse(value),
            TypeCode.UInt64 => UInt64.Parse(value),
            TypeCode.Single => Single.Parse(value),
            TypeCode.Double => Double.Parse(value),
            TypeCode.Decimal => Decimal.Parse(value),
            TypeCode.DateTime => DateTime.Parse(value),
            TypeCode.String => value,
            _ => HandleUnknown(type, value),
        });

        static object? HandleObject(Type type, string value)
        {
            if (type == typeof(Guid))
            {
                return Guid.Parse(value);
            }

            throw new NotImplementedException();
        }

        // ReSharper restore BuiltInTypeReferenceStyleForMemberAccess

        static object? HandleUnknown(Type type, string value)
        {

            throw new NotImplementedException();
        }
    }

    public static MethodCallExpression? BuildEfFunctionsLikeExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType != typeof(string)) return null;

        var valueConstant = Expression.Constant(value, typeof(string));
        var efFunctionsConstant = Expression.Constant(EF.Functions, typeof(DbFunctions));

        return Expression.Call(null, EfFunctionsLikeMethodInfo, efFunctionsConstant, memberExpr, valueConstant);
    }

    public static MethodCallExpression? BuildEfFunctionsCollatedILikeExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType != typeof(string)) return null;

        var valueConstant = Expression.Constant(value, typeof(string));
        var defaultStrConstant = Expression.Constant("default", typeof(string));
        var efFunctionsConstant = Expression.Constant(EF.Functions, typeof(DbFunctions));

        var collated = Expression.Call(null, EfFunctionsCollateMethodInfo, efFunctionsConstant, memberExpr, defaultStrConstant);

        return Expression.Call(null, EfFunctionsILikeMethodInfo, efFunctionsConstant, collated, valueConstant);
    }

    public static BinaryExpression BuildEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        return Expression.Equal(memberExpr, GetConstant(memberType, value));
    }

    public static BinaryExpression BuildNotEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        return Expression.NotEqual(memberExpr, GetConstant(memberType, value));
    }

    public static BinaryExpression? BuildLessThanExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false } && Type.GetTypeCode(memberType) != TypeCode.DateTime) return null;
        return Expression.LessThan(memberExpr, GetConstant(memberType, value));
    }

    public static BinaryExpression? BuildGreaterThanExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false } && Type.GetTypeCode(memberType) != TypeCode.DateTime) return null;
        return Expression.GreaterThan(memberExpr, GetConstant(memberType, value));
    }

    public static BinaryExpression? BuildLessThanOrEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false } && Type.GetTypeCode(memberType) != TypeCode.DateTime) return null;
        return Expression.LessThanOrEqual(memberExpr, GetConstant(memberType, value));
    }

    public static BinaryExpression? BuildGreaterThanOrEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false } && Type.GetTypeCode(memberType) != TypeCode.DateTime) return null;
        return Expression.GreaterThanOrEqual(memberExpr, GetConstant(memberType, value));
    }
}
