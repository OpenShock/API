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
        await Assert.That(result.AsT0).IsTypeOf<OneOf.Types.Success>();
    }

    [Test]
    public async Task Validate_TooShort_ReturnsError()
    {
        // Arrange
        string username = "aa";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.TooShort).IsTrue();
    }

    [Test]
    public async Task Validate_TooLong_ReturnsError()
    {
        // Arrange
        string username = new('a', 33);

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.TooLong).IsTrue();
    }

    [Test]
    public async Task Validate_StartWithWhitespace_ReturnsError()
    {
        // Arrange
        string username = " TestUser123";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.StartOrEndWithWhitespace).IsTrue();
    }

    [Test]
    public async Task Validate_EndWithWhitespace_ReturnsError()
    {
        // Arrange
        string username = "TestUser123 ";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.StartOrEndWithWhitespace).IsTrue();
    }

    [Test]
    public async Task Validate_ResembleEmail_ReturnsError()
    {
        // Arrange
        string username = "test@domain.com";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.ResembleEmail).IsTrue();
    }

    [Test]
    public async Task Validate_ContainsObnoxiousCharacters_ReturnsError()
    {
        // Arrange
        string username = "TestUser😀";

        // Act
        var result = UsernameValidator.Validate(username);

        // Assert
        await Assert.That(result.AsT1).IsTypeOf<UsernameError>();
        await Assert.That(result.AsT1.Type == UsernameErrorType.ObnoxiousCharacters).IsTrue();
    }
}
