using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.Utils;

public static partial class IQueryableExtensions
{
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex ValidMemberNameRegex();
    
    private enum OperationType
    {
        None,
        Equals,
        StartsWith,
        EndsWith,
        Contains,
    }
    
    private static readonly ConstantExpression DefaultStrConstExpr = Expression.Constant("default", typeof(string));
    private static readonly ConstantExpression EfFunctionsConstExpr = Expression.Constant(EF.Functions, typeof(DbFunctions));
    private static readonly MethodInfo EfFunctionsCollateMethodInfo = typeof(RelationalDbFunctionsExtensions).GetMethod("Collate")?.MakeGenericMethod(typeof(string)) ?? throw new NullReferenceException("EF.Functions.Collate(string,string) not found");
    private static readonly MethodInfo EfFunctionsILikeMethodInfo = typeof(NpgsqlDbFunctionsExtensions).GetMethod("ILike", [typeof(DbFunctions), typeof(string), typeof(string) ]) ?? throw new NullReferenceException("EF.Functions.ILike(string,string) not found");
    private static readonly MethodInfo StringEqualsMethodInfo = typeof(string).GetMethod("Equals", [typeof(string)]) ?? throw new Exception("string.Equals(string,StringComparison) method not found");
    private static readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethod("StartsWith", [typeof(string)]) ?? throw new Exception("string.StartsWith(string) method not found");
    private static readonly MethodInfo StringEndsWithMethodInfo = typeof(string).GetMethod("EndsWith", [typeof(string)]) ?? throw new Exception("string.EndsWith(string) method not found");
    private static readonly MethodInfo StringContainsMethodInfo = typeof(string).GetMethod("Contains", [typeof(string)]) ?? throw new Exception("string.Contains(string) method not found");
    
    private static MemberInfo? GetPropertyOrField(Type type, string propOrFieldName)
    {
        var member = type.GetMember(propOrFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.IgnoreCase).SingleOrDefault();
        if (member == null) return null;

        var isIgnored = member.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any();
        if (isIgnored) return null;

        return member;
    }

    private static Type? GetPropertyOrFieldType(MemberInfo propOrField)
    {
        return propOrField switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => null
        };
    }

    private static Expression<Func<T, bool>>? CreatePropertyOrFieldOperationExpression<T>(string memberName, Func<MemberExpression, Type, Expression> createOperationExpression)
    {
        var entityType = typeof(T);

        var memberInfo = GetPropertyOrField(entityType, memberName);
        if (memberInfo == null) return null;

        var memberType = GetPropertyOrFieldType(memberInfo);
        if (memberType == null) return null;

        var parameterExpr = Expression.Parameter(entityType, "x");
        var memberExpr = Expression.MakeMemberAccess(parameterExpr, memberInfo);

        var operationExpr = createOperationExpression(memberExpr, memberType);

        return Expression.Lambda<Func<T, bool>>(operationExpr, parameterExpr);
    }

    private static Func<MemberExpression, Type, Expression> CreatePgsqlILikeOperationExpression(string value)
    {
        var valueConstant = Expression.Constant(value, typeof(string));

        return (memberExpr, memberType) =>
        {
            var collated = Expression.Call(null, EfFunctionsCollateMethodInfo, EfFunctionsConstExpr, memberExpr, DefaultStrConstExpr);
            return Expression.Call(null, EfFunctionsILikeMethodInfo, EfFunctionsConstExpr, collated, valueConstant);
        };
    }
    
    private static Func<MemberExpression, Type, Expression> CreateStringEqualsOperationExpression(string value)
    {
        var valueConstant = Expression.Constant(value, typeof(string));
        
        return (memberExpr, memberType) => Expression.Call(memberExpr, StringEqualsMethodInfo, valueConstant);
    }

    private static Func<MemberExpression, Type, Expression> CreateStringStartsWithOperationExpression(string value)
    {
        var valueConstant = Expression.Constant(value, typeof(string));
        
        return (memberExpr, memberType) => Expression.Call(memberExpr, StringStartsWithMethodInfo, valueConstant);
    }

    private static Func<MemberExpression, Type, Expression> CreateStringEndsWithOperationExpression(string value)
    {
        var valueConstant = Expression.Constant(value, typeof(string));
        
        return (memberExpr, memberType) => Expression.Call(memberExpr, StringEndsWithMethodInfo, valueConstant);
    }

    private static Func<MemberExpression, Type, Expression> CreateStringContainsOperationExpression(string value)
    {
        var valueConstant = Expression.Constant(value, typeof(string));
        
        return (memberExpr, memberType) => Expression.Call(memberExpr, StringContainsMethodInfo, valueConstant);
    }

    private static Expression<Func<T, bool>>? CreatePropertyOrFieldStringCompareExpression<T>(string propOrFieldName, OperationType operation, string value) where T : class
    {
        var operationExpression = operation switch
        {
            OperationType.Equals => CreatePgsqlILikeOperationExpression(value),
            OperationType.StartsWith => CreatePgsqlILikeOperationExpression($"{value}%"),
            OperationType.EndsWith => CreatePgsqlILikeOperationExpression($"%{value}"),
            OperationType.Contains => CreatePgsqlILikeOperationExpression($"%{value}%"),
            _ => throw new Exception("Unsupported operation!")
        };

        return CreatePropertyOrFieldOperationExpression<T>(propOrFieldName, operationExpression);
    }

    private static List<string> GetFilterWords(ReadOnlySpan<char> query)
    {
        query = query.Trim();

        int index;
        List<string> words = [];
        while (!query.IsEmpty)
        {
            if (query[0] == '\'')
            {
                // Remove quote
                query = query[1..];

                // Look for next quote
                index = query.IndexOf('\'');
                if (index < 0) throw new Exception("Invalid query string, unterminated quote found.");

                // Return string
                words.Add(query[..index].ToString());

                // Remove string
                query = query[(index + 1)..].TrimStart(' ');
            }
            else
            {
                // Look for space
                index = query.IndexOf(' ');
                if (index < 0)
                {
                    // No more spaces, return last word
                    words.Add(query.ToString());
                    break;
                }

                // Return next word
                words.Add(query[..index].ToString());

                // Remove word and spaces behind
                query = query[(index + 1)..].TrimStart(' ');
            }
        }

        return words;
    }

    private sealed record ParsedFilter(string MemberName, OperationType Operation, string Value);
    private enum ExpectedToken
    {
        Member,
        Operation,
        Value,
        AndOrEnd
    }
    private static IEnumerable<ParsedFilter> ParseFilters(string query)
    {
        string member = string.Empty;
        OperationType operation = OperationType.None;
        ExpectedToken expectedToken = ExpectedToken.Member;
        foreach (var word in GetFilterWords(query))
        {
            switch (expectedToken)
            {
                case ExpectedToken.Member:
                    member = word;
                    expectedToken = ExpectedToken.Operation;
                    break;
                case ExpectedToken.Operation:
                    operation = word switch
                    {
                        "==" or "===" => OperationType.Equals,
                        "=*" or "==*" => OperationType.StartsWith,
                        "*=" or "*==" => OperationType.EndsWith,
                        "=*=" => OperationType.Contains,
                        _ => throw new Exception("Unsupported operation!")
                    };
                    expectedToken = ExpectedToken.Value;
                    break;
                case ExpectedToken.Value:
                    if (!ValidMemberNameRegex().IsMatch(member)) throw new Exception("Invalid filter string!");
                    if (operation == OperationType.None) throw new Exception("Invalid filter string!");
                    
                    yield return new ParsedFilter(member, operation, word);
                    
                    member = string.Empty;
                    operation = OperationType.None;
                    expectedToken = ExpectedToken.AndOrEnd;
                    break;
                case ExpectedToken.AndOrEnd:
                    if (word != "and") throw new Exception("Only and is supported atm!");
                    expectedToken = ExpectedToken.Member;
                    break;
                default:
                    throw new Exception("Unexpected state!");
            }
        }

        if (expectedToken != ExpectedToken.AndOrEnd)
        {
            throw new Exception("Unexpected end of query");
        }
    }

    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string filterQuery) where T : class
    {
        foreach (var filter in ParseFilters(filterQuery))
        {
            var containsExpression = CreatePropertyOrFieldStringCompareExpression<T>(filter.MemberName, filter.Operation, filter.Value);

            if (containsExpression != null)
            {
                query = query.Where(containsExpression);
            }
        }

        return query;
    }

    public static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, string orderbyQuery) where T : class
    {


        return query;
    }
}
