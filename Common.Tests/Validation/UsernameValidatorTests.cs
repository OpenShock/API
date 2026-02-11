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

    // --- Boundary value tests ---

    [Test]
    public async Task Validate_ExactMinLength_ReturnsSuccess()
    {
        // HardLimits.UsernameMinLength = 3
        var result = UsernameValidator.Validate("abc");
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_OneBelowMinLength_ReturnsTooShort()
    {
        // 2 chars = below min of 3
        var result = UsernameValidator.Validate("ab");
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.TooShort);
    }

    [Test]
    public async Task Validate_ExactMaxLength_ReturnsSuccess()
    {
        // HardLimits.UsernameMaxLength = 32
        var result = UsernameValidator.Validate(new string('a', 32));
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_OneAboveMaxLength_ReturnsTooLong()
    {
        // 33 chars = above max of 32
        var result = UsernameValidator.Validate(new string('a', 33));
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.TooLong);
    }

    [Test]
    public async Task Validate_WithHyphensAndUnderscores_ReturnsSuccess()
    {
        var result = UsernameValidator.Validate("test-user_123");
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_WithDots_ReturnsSuccess()
    {
        var result = UsernameValidator.Validate("test.user");
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_WithMiddleSpaces_ReturnsSuccess()
    {
        // Middle spaces are allowed, only leading/trailing are rejected
        var result = UsernameValidator.Validate("test user");
        await Assert.That(result.IsT0).IsTrue();
    }

    [Test]
    public async Task Validate_TabAtStart_ReturnsWhitespaceError()
    {
        var result = UsernameValidator.Validate("\tTestUser");
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.StartOrEndWithWhitespace);
    }

    [Test]
    public async Task Validate_AtSignInMiddle_ReturnsResembleEmail()
    {
        var result = UsernameValidator.Validate("user@name");
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.ResembleEmail);
    }

    [Test]
    public async Task Validate_ZeroWidthJoiner_ReturnsObnoxious()
    {
        // Zero-width joiner U+200D
        var result = UsernameValidator.Validate("test\u200Duser");
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.ObnoxiousCharacters);
    }

    [Test]
    public async Task Validate_RightToLeftOverride_ReturnsObnoxious()
    {
        // U+202E Right-to-left override
        var result = UsernameValidator.Validate("test\u202Euser");
        await Assert.That(result.IsT1).IsTrue();
        await Assert.That(result.AsT1.Type).IsEqualTo(UsernameErrorType.ObnoxiousCharacters);
    }
}
