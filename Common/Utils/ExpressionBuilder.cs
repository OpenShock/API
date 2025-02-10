using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.Utils;

public static partial class ExpressionBuilder
{
    public sealed class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message) { }
    }
    
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex ValidMemberNameRegex();
    
    private static readonly MethodInfo EfFunctionsCollateMethodInfo = typeof(RelationalDbFunctionsExtensions).GetMethod("Collate")?.MakeGenericMethod(typeof(string)) ?? throw new ExpressionException("EF.Functions.Collate(string,string) not found");
    private static readonly MethodInfo EfFunctionsILikeMethodInfo = typeof(NpgsqlDbFunctionsExtensions).GetMethod("ILike", [typeof(DbFunctions), typeof(string), typeof(string) ]) ?? throw new ExpressionException("EF.Functions.ILike(string,string) not found");
    private static readonly MethodInfo StringEqualsMethodInfo = typeof(string).GetMethod("Equals", [typeof(string)]) ?? throw new ExpressionException("string.Equals(string,StringComparison) method not found");
    private static readonly MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethod("StartsWith", [typeof(string)]) ?? throw new ExpressionException("string.StartsWith(string) method not found");
    private static readonly MethodInfo StringEndsWithMethodInfo = typeof(string).GetMethod("EndsWith", [typeof(string)]) ?? throw new ExpressionException("string.EndsWith(string) method not found");
    private static readonly MethodInfo StringContainsMethodInfo = typeof(string).GetMethod("Contains", [typeof(string)]) ?? throw new ExpressionException("string.Contains(string) method not found");
    
    public static MemberInfo? GetPropertyOrField(Type type, string propOrFieldName)
    {
        var member = type.GetMember(propOrFieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.IgnoreCase).SingleOrDefault();
        if (member == null)
            return null;

        var isIgnored = member.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any();
        if (isIgnored)
            return null;

        return member;
    }

    public static Type? GetPropertyOrFieldType(MemberInfo propOrField)
    {
        return propOrField switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => null
        };
    }

    private static ConstantExpression GetConstant(Type type, string value)
    {
        /* Currently this causes a really weird bug which persists across subsequent requests
        if (type.IsEnum)
        {
            var enumValue = Enum.Parse(type, value, ignoreCase: true);
            return Expression.Constant(enumValue, type);
        }
        */

        return Expression.Constant(value, type);
    }

    private static Expression BuildEfFunctionsCollatedILikeExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType != typeof(string))
            throw new ExpressionException($"Operation ILIKE is not supported for {memberType}");

        var valueConstant = Expression.Constant(value, typeof(string));
        var defaultStrConstant = Expression.Constant("default", typeof(string));
        var efFunctionsConstant = Expression.Constant(EF.Functions, typeof(DbFunctions));

        var collated = Expression.Call(null, EfFunctionsCollateMethodInfo, efFunctionsConstant, memberExpr, defaultStrConstant);

        return Expression.Call(null, EfFunctionsILikeMethodInfo, efFunctionsConstant, collated, valueConstant);
    }

    private static Expression BuildEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        return Expression.Equal(memberExpr, GetConstant(memberType, value));
    }

    private static Expression BuildNotEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        return Expression.NotEqual(memberExpr, GetConstant(memberType, value));
    }

    private static Expression BuildLessThanExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false })
            throw new ExpressionException($"Operation < is not supported for {memberType}");
        return Expression.LessThan(memberExpr, GetConstant(memberType, value));
    }

    private static Expression BuildGreaterThanExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false })
            throw new ExpressionException($"Operation > is not supported for {memberType}");
        return Expression.GreaterThan(memberExpr, GetConstant(memberType, value));
    }

    private static Expression BuildLessThanOrEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false })
            throw new ExpressionException($"Operation <= is not supported for {memberType}");
        return Expression.LessThan(memberExpr, GetConstant(memberType, value));
    }

    private static Expression BuildGreaterThanOrEqualExpression(Type memberType, Expression memberExpr, string value)
    {
        if (memberType is { IsPrimitive: false, IsEnum: false })
            throw new ExpressionException($"Operation >= is not supported for {memberType}");
        return Expression.GreaterThan(memberExpr, GetConstant(memberType, value));
    }

    private static Expression CreateMemberCompareExpression<T>(Type entityType, ParameterExpression parameterExpr, string propOrFieldName, string operation, string value) where T : class
    {
        var memberInfo = GetPropertyOrField(entityType, propOrFieldName);
        if (memberInfo == null)
            throw new ExpressionException($"'{propOrFieldName}' is not a valid property");

        var memberType = GetPropertyOrFieldType(memberInfo);
        if (memberType == null)
            throw new ExpressionException("Unknown error occured");

        var memberExpr = Expression.MakeMemberAccess(parameterExpr, memberInfo);

        return operation switch
        {
            "like" => BuildEfFunctionsCollatedILikeExpression(memberType, memberExpr, value),
            "==" or "eq" => BuildEqualExpression(memberType, memberExpr, value),
            "!=" or "neq" => BuildNotEqualExpression(memberType, memberExpr, value),
            "<" or "lt" => BuildLessThanExpression(memberType, memberExpr, value),
            ">" or "gt" => BuildGreaterThanExpression(memberType, memberExpr, value),
            "<=" or "lte" => BuildLessThanOrEqualExpression(memberType, memberExpr, value),
            ">=" or "gte" => BuildGreaterThanOrEqualExpression(memberType, memberExpr, value),
            _ => throw new ExpressionException($"'{operation}' is not a supported operation type.")
        };
    }

    public readonly record struct ParseResult(string Value, int Consumed);
    static ParseResult ParseQuotedString(ReadOnlySpan<char> input)
    {
        const char QuoteChar = '\'';
        const char EscapeChar = '\\';

        if (input.Length <= 0)
            throw new FormatException("Closing quote not found.");

        if (input.Length == 1)
        {
            if (input[0] != '\'')
                throw new FormatException("Closing quote not found.");

            return new ParseResult(string.Empty, 1);
        }

        // Look for either a backslash or a closing quote.
        int firstOccurrence = input.IndexOfAny(QuoteChar, EscapeChar);
        if (firstOccurrence == -1)
            throw new FormatException("Closing quote not found.");

        // Fast path: if the first occurrence is a closing quote and no escapes were encountered.
        if (input[firstOccurrence] == QuoteChar)
        {
            // Return the substring with the closing quote consumed.
            return new ParseResult(input[..firstOccurrence].ToString(), firstOccurrence + 1);
        }

        // Otherwise, fall back to the slower character-by-character parse.
        var sb = new StringBuilder();

        int i = firstOccurrence, start = firstOccurrence;

        if (firstOccurrence > 0) sb.Append(input[..i]);

        while (i < input.Length)
        {
            char c = input[i];
            if (c == QuoteChar)
            {
                if (i > start) sb.Append(input[start..i]);

                // Return the substring with the closing quote consumed.
                return new ParseResult(sb.ToString(), i + 1);
            }
            else if (c == '\\')
            {
                if (i > start) sb.Append(input[start..i]);

                if (++i >= input.Length)
                    throw new FormatException("Incomplete escape sequence at end of input.");

                sb.Append(input[i++] switch
                {
                    QuoteChar => QuoteChar,
                    EscapeChar => EscapeChar,
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw new FormatException("Invalid escape sequence.")
                });

                start = i;
            }
            else
            {
                i++;
            }
        }

        throw new FormatException("Closing quote not found in input.");
    }


    private static List<string> GetFilterWords(ReadOnlySpan<char> query)
    {
        query = query.Trim();

        List<string> words = [];
        while (!query.IsEmpty)
        {
            int index;
            if (query[0] == '\'')
            {
                var result = ParseQuotedString(query[1..]);
                query = query[(result.Consumed + 1)..];
                words.Add(result.Value);
                continue;
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
            }

            // Return next word
            words.Add(query[..index].ToString());

            // Remove word and spaces behind
            query = query[(index + 1)..].TrimStart(' ');
        }

        return words;
    }

    private sealed record ParsedFilter(string MemberName, string Operation, string Value);
    private enum ExpectedToken
    {
        Member,
        Operation,
        Value,
        AndOrEnd
    }
    private static IEnumerable<ParsedFilter> ParseFilters(string query)
    {
        var member = string.Empty;
        var operation = string.Empty;
        var expectedToken = ExpectedToken.Member;
        foreach (var word in GetFilterWords(query))
        {
            switch (expectedToken)
            {
                case ExpectedToken.Member:
                    member = word;
                    expectedToken = ExpectedToken.Operation;
                    break;
                case ExpectedToken.Operation:
                    operation = word;
                    expectedToken = ExpectedToken.Value;
                    break;
                case ExpectedToken.Value:
                    if (!ValidMemberNameRegex().IsMatch(member))
                        throw new ExpressionException("Invalid filter string!");

                    if (string.IsNullOrEmpty(operation))
                        throw new ExpressionException("Invalid filter string!");

                    yield return new ParsedFilter(member, operation, word);

                    member = string.Empty;
                    operation = string.Empty;
                    expectedToken = ExpectedToken.AndOrEnd;
                    break;
                case ExpectedToken.AndOrEnd:
                    if (word != "and") throw new ExpressionException("Only and is supported atm!");
                    expectedToken = ExpectedToken.Member;
                    break;
                default:
                    throw new ExpressionException("Unexpected state!");
            }
        }

        if (expectedToken != ExpectedToken.AndOrEnd)
            throw new ExpressionException("Unexpected end of query");
    }

    public static Expression<Func<T, bool>>? GetFilterExpression<T>(string filterQuery) where T : class
    {
        Expression? completeExpr = null;

        var entityType = typeof(T);
        var parameterExpr = Expression.Parameter(entityType, "x");

        foreach (var filter in ParseFilters(filterQuery))
        {
            var memberExpr = CreateMemberCompareExpression<T>(entityType, parameterExpr, filter.MemberName, filter.Operation, filter.Value);

            if (completeExpr == null)
            {
                completeExpr = memberExpr;
            }
            else
            {
                completeExpr = Expression.And(completeExpr, memberExpr);
            }
        }

        if (completeExpr == null) return null;

        return Expression.Lambda<Func<T, bool>>(completeExpr, parameterExpr);
    }
}
