using OpenShock.Common.Query;
using Bogus;

namespace OpenShock.Common.Tests.Query;

public class ExpressionBuilderTests
{
    public sealed class TestClass
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required int Age { get; set; }
        public required uint Height { get; set; }
        public required bool IsActive { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required TestEnum Status { get; set; }
        public required float Score { get; set; }
        public required double Precision { get; set; }
    }

    public enum TestEnum
    {
        Pending,
        Active,
        Inactive
    }

    private readonly TestClass[] TestArray;

    public ExpressionBuilderTests()
    {
        var faker = new Faker<TestClass>()
            .UseSeed(12345)
            .RuleFor(t => t.Id, f => Guid.CreateVersion7())
            .RuleFor(t => t.Name, f => f.Name.FullName())
            .RuleFor(t => t.Age, f => f.Random.Int(18, 99))
            .RuleFor(t => t.Height, f => f.Random.UInt())
            .RuleFor(t => t.IsActive, f => f.Random.Bool())
            .RuleFor(t => t.CreatedAt, f => f.Date.Past(10))
            .RuleFor(t => t.Status, f => f.PickRandom<TestEnum>())
            .RuleFor(t => t.Score, f => f.Random.Float(0, 100))
            .RuleFor(t => t.Precision, f => f.Random.Double(0, 100));

        TestArray = faker.Generate(100).ToArray();
    }

    [Test]
    public async Task EmptyString_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>(""))
            .ThrowsExactly<DBExpressionBuilderException>();
    }

    [Test]
    public async Task IntegerBounds_ThrowsExceptionOnOverflow()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("age eq 2147483648"))
            .ThrowsExactly<OverflowException>();
    }

    [Test]
    public async Task UnsignedIntegerBounds_ThrowsExceptionOnNegative()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("height eq -1"))
            .ThrowsExactly<OverflowException>();
    }

    [Test]
    public async Task Guid_ExactMatch()
    {
        // Act
        var testGuid = TestArray.First().Id; // Grab a Guid from the test data
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>($"id eq {testGuid}");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Id == testGuid);
    }

    [Test]
    public async Task Integer_GreaterThanOrEquals()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("age gte 42");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Age >= 42);
    }

    [Test]
    public async Task Integer_LessThanOrEquals()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("age lte 51");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Age <= 51);
    }

    [Test]
    public async Task Enum_Equals_ChecksValidValues()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("status eq Active");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Status == TestEnum.Active);
    }

    [Test]
    public async Task Enum_NotEquals_ChecksValidValues()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("status != Active");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).DoesNotContain(x => x.Status == TestEnum.Active);
    }

    [Test]
    public async Task Enum_InvalidValue_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("status eq Invalid"))
            .ThrowsExactly<FormatException>();
    }

    [Test]
    public async Task Boolean_TrueMatches()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("isActive eq true");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.IsActive);
    }

    [Test]
    public async Task Boolean_FalseMatches()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("isActive eq false");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.IsActive == false);
    }

    [Test]
    public async Task DateTime_ExactMatch()
    {
        // Act
        var testDate = TestArray[20].CreatedAt;
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>($"createdAt eq {testDate:O}");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.CreatedAt == testDate);
    }

    [Test]
    public async Task DateTime_LessThan()
    {
        // Act
        var referenceDate = DateTime.UtcNow.AddMonths(-6);
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>($"createdAt lt {referenceDate:O}");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.CreatedAt < referenceDate);
    }

    [Test]
    public async Task Float_GreaterThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("score gt 50");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Score > 50f);
    }

    [Test]
    public async Task Double_LessThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("precision lt 50");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).ContainsOnly(x => x.Precision < 50f);
    }

    [Test]
    [Arguments("Age")]
    [Arguments("age asc")]
    [Arguments("AGE ASC")]
    public async Task OrderByAscending_SortsCorrectly(string query)
    {
        // Arrange
        var queryable = TestArray.AsQueryable();

        // Act
        var result = queryable.ApplyOrderBy(query).ToArray();

        // Assert
        await Assert.That(result).IsOrderedBy(x => x.Age);
    }

    [Test]
    [Arguments("name desc")]
    [Arguments("NAME DESC")]
    public async Task OrderByDescending_SortsCorrectly(string query)
    {
        // Arrange
        var queryable = TestArray.AsQueryable();

        // Act
        var result = queryable.ApplyOrderBy(query).ToArray();

        // Assert
        await Assert.That(result).IsOrderedByDescending(x => x.Name);
    }

    [Test]
    [Arguments("age,createdat")]
    [Arguments("age asc,createdat asc")]
    public async Task ThenByAscending_SortsCorrectly(string query)
    {
        // Arrange
        var queryable = TestArray.AsQueryable();

        // Act
        var result = queryable.ApplyOrderBy(query).ToArray();

        // Assert
        var expected = TestArray.OrderBy(x => x.Age).ThenBy(x => x.CreatedAt).ToArray();
        await Assert.That(result).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments("age,name desc")]
    public async Task ThenByDescending_SortsCorrectly(string query)
    {
        // Arrange
        var queryable = TestArray.AsQueryable();

        // Act
        var result = queryable.ApplyOrderBy(query).ToArray();

        // Assert
        var expected = TestArray.OrderBy(x => x.Age).ThenByDescending(x => x.Name).ToArray();
        await Assert.That(result).IsEquivalentTo(expected);
    }
}
