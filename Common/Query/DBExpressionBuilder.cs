using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace OpenShock.Common.Query;

public sealed class DBExpressionBuilderException : Exception
{
    public DBExpressionBuilderException(string message) : base(message) { }
}

public static partial class DBExpressionBuilder
{
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*$")]
    private static partial Regex ValidMemberNameRegex();

    private static Expression CreateMemberCompareExpression(Type entityType, ParameterExpression parameterExpr, string propOrFieldName, string operation, string value)
    {
        var (memberInfo, memberType) = DBExpressionBuilderUtils.GetPropertyOrField(entityType, propOrFieldName);

        var memberExpr = Expression.MakeMemberAccess(parameterExpr, memberInfo);

        // ReSharper disable once StringLiteralTypo
        Expression? resultExpr = operation switch
        {
            //"like" => DBExpressionBuilderUtils.BuildEfFunctionsLikeExpression(memberType, memberExpr, value),
            "ilike" => DBExpressionBuilderUtils.BuildEfFunctionsCollatedILikeExpression(memberType, memberExpr, value),
            "==" or "eq" => DBExpressionBuilderUtils.BuildEqualExpression(memberType, memberExpr, value),
            "!=" or "neq" => DBExpressionBuilderUtils.BuildNotEqualExpression(memberType, memberExpr, value),
            "<" or "lt" => DBExpressionBuilderUtils.BuildLessThanExpression(memberType, memberExpr, value),
            ">" or "gt" => DBExpressionBuilderUtils.BuildGreaterThanExpression(memberType, memberExpr, value),
            "<=" or "lte" => DBExpressionBuilderUtils.BuildLessThanOrEqualExpression(memberType, memberExpr, value),
            ">=" or "gte" => DBExpressionBuilderUtils.BuildGreaterThanOrEqualExpression(memberType, memberExpr, value),
            _ => throw new DBExpressionBuilderException($"'{operation}' is not a supported operation type.")
        };
        
        return resultExpr ?? throw new DBExpressionBuilderException($"Operation {operation} is not supported for {memberType}");
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
        foreach (var word in QueryStringTokenizer.ParseQueryTokens(query))
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
                        throw new DBExpressionBuilderException("Invalid filter string!");

                    if (string.IsNullOrEmpty(operation))
                        throw new DBExpressionBuilderException("Invalid filter string!");

                    yield return new ParsedFilter(member, operation, word);

                    member = string.Empty;
                    operation = string.Empty;
                    expectedToken = ExpectedToken.AndOrEnd;
                    break;
                case ExpectedToken.AndOrEnd:
                    if (word != "and") throw new DBExpressionBuilderException("Only and is supported atm!");
                    expectedToken = ExpectedToken.Member;
                    break;
                default:
                    throw new DBExpressionBuilderException("Unexpected state!");
            }
        }

        if (expectedToken != ExpectedToken.AndOrEnd)
            throw new DBExpressionBuilderException("Unexpected end of query");
    }

    public static Expression<Func<T, bool>> GetFilterExpression<T>(string filterQuery) where T : class
    {
        var entityType = typeof(T);
        var parameterExpr = Expression.Parameter(entityType, "x");

        var completeExpr = ParseFilters(filterQuery)
            .Select(filter => CreateMemberCompareExpression(entityType, parameterExpr, filter.MemberName, filter.Operation, filter.Value))
            .Aggregate<Expression, Expression?>(null, (prev, next) => prev == null ? next : Expression.And(prev, next));

        return Expression.Lambda<Func<T, bool>>(completeExpr ?? Expression.Constant(true), parameterExpr);
    }
}
