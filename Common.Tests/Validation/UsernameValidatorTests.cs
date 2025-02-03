using OpenShock.Common.Validation;

namespace OpenShock.Common.Tests.Validation;

internal class UsernameValidatorTests
{
    [Test]
    public async Task Validate_ValidUsername_ReturnsSuccess()
    {
        // Arrange
        string username = "TestUser123";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_TooShort_ReturnsError()
    {
        // Arrange
        string username = "aa";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.TooShort);
    }

    [Test]
    public async Task Validate_TooLong_ReturnsError()
    {
        // Arrange
        string username = new('a', 33);

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.TooLong);
    }

    [Test]
    public async Task Validate_StartWithWhitespace_ReturnsError()
    {
        // Arrange
        string username = " TestUser123";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.StartOrEndWithWhitespace);
    }

    [Test]
    public async Task Validate_EndWithWhitespace_ReturnsError()
    {
        // Arrange
        string username = "TestUser123 ";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.StartOrEndWithWhitespace);
    }

    [Test]
    public async Task Validate_ResembleEmail_ReturnsError()
    {
        // Arrange
        string username = "test@domain.com";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.ResembleEmail);
    }

    [Test]
    public async Task Validate_ContainsObnoxiousCharacters_ReturnsError()
    {
        // Arrange
        string username = "TestUser😀";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.ObnoxiousCharacters);
    }
}
