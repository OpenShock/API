using OpenShock.Common.Query;
using TUnit.Assertions.AssertConditions.Throws;
using Bogus;

namespace OpenShock.Common.Tests.Query;

public class DBExpressionBuilderTests
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

    public DBExpressionBuilderTests()
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
        await Assert.That(result).HasCount().EqualTo(1); // Should only match the single Guid
    }

    // TODO: Make enums work
    /*
    [Test]
    public async Task Enum_ChecksValidValues()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("status eq Active");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }

    [Test]
    public async Task Enum_InvalidValue_ThrowsException()
    {
        // Act & Assert
        await Assert
            .That(() => DBExpressionBuilder.GetFilterExpression<TestClass>("status eq Invalid"))
            .ThrowsExactly<DBExpressionBuilderException>();
    }
    */

    [Test]
    public async Task Boolean_TrueMatches()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("isActive eq true");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }

    [Test]
    public async Task Boolean_FalseMatches()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("isActive eq false");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }

    [Test]
    public async Task DateTime_ExactMatch()
    {
        // Act
        var testDate = TestArray.First().CreatedAt;
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>($"createdAt eq {testDate:O}");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().EqualTo(1);
    }

    [Test]
    public async Task DateTime_LessThan()
    {
        // Act
        var referenceDate = DateTime.UtcNow;
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>($"createdAt lt {referenceDate:O}");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }

    [Test]
    public async Task Float_GreaterThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("score gt 50");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }

    [Test]
    public async Task Double_LessThan()
    {
        // Act
        var expression = DBExpressionBuilder.GetFilterExpression<TestClass>("precision lt 50");
        var result = TestArray.AsQueryable().Where(expression).ToArray();

        // Assert
        await Assert.That(result).HasCount().GreaterThan(0);
    }
}
