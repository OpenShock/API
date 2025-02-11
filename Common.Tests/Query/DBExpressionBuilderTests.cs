using OpenShock.Common.Query;
using TUnit.Assertions.AssertConditions.Throws;

namespace OpenShock.Common.Tests.Query;

public class DBExpressionBuilderTests
{
    public sealed class TestClass
    {
        public required string StringProp { get; set; }
        public required int NumberProp { get; set; }
    }

    private readonly TestClass[] TestArray =
    [
        new() { StringProp = "ASD", NumberProp = 10 },
        new() { StringProp = "SDF", NumberProp = 20 },
        new() { StringProp = "XYZ", NumberProp = 30 }
    ];

    [Test]
    public async Task EmptyString_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>(""))
            .ThrowsExactly<DBExpressionBuilderException>();
    }

    [Test]
    public async Task EqOperator_ReturnsMatching()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("stringprop eq ASD");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(1);
    }

    [Test]
    public async Task NeqOperator_ReturnsNonMatching()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("stringprop neq ASD");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(2);
    }

    [Test]
    public async Task LtOperator_ReturnsLessThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("numberprop lt 20");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(1);
    }

    [Test]
    public async Task LteOperator_ReturnsLessThanOrEqual()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("numberprop lte 20");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(2);
    }

    [Test]
    public async Task GtOperator_ReturnsGreaterThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("numberprop gt 20");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(1);
    }

    [Test]
    public async Task GteOperator_ReturnsGreaterThanOrEqual()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("numberprop gte 20");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(2);
    }

    [Test]
    public async Task MultipleConditions_WithAnd_ReturnsMatching()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("stringprop eq SDF and numberprop eq 20");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(1);
    }

    [Test]
    public async Task InvalidQuery_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("invalid query"))
            .ThrowsExactly<DBExpressionBuilderException>();
    }

    [Test]
    public async Task UnsupportedOperator_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("stringprop contains value"))
            .ThrowsExactly<DBExpressionBuilderException>();
    }
}
