using OpenShock.Common.Geo;
using TUnit.Assertions.AssertConditions.Throws;

namespace OpenShock.Common.Tests.Geo;

public class Alpha2CountryCodeTests
{
    [Test]
    [Arguments("US", 'U', 'S')]
    [Arguments("DE", 'D', 'E')]
    public async Task ValidCode_ShouldParse(string str, char char1, char char2)
    {
        // Act
        Alpha2CountryCode c = str;

        // Assert
        await Assert.That(c.Char1).IsEqualTo(char1);
        await Assert.That(c.Char2).IsEqualTo(char2);
    }

    [Test]
    [Arguments("E")]
    [Arguments("INVALID")]
    public async Task InvalidCharCount_ShouldThrow_InvalidLength(string str)
    {
        // Act & Assert
        await Assert.That(() =>
            {
                Alpha2CountryCode c = str;
            })
            .ThrowsExactly<ArgumentOutOfRangeException>()
            .WithMessage("Country code must be exactly 2 uppercase ASCII characters (Parameter 'str')");
    }

    [Test]
    [Arguments("us")]
    [Arguments("Us")]
    [Arguments("uS")]
    [Arguments("12")]
    [Arguments("U1")]
    [Arguments("1U")]
    [Arguments("ÆØ")]
    [Arguments(":D")]
    public async Task InvalidCharTypes_ShouldThrow(string str)
    {
        // Act & Assert
        await Assert.That(() =>
            {
                Alpha2CountryCode c = str;
            })
            .ThrowsExactly<ArgumentOutOfRangeException>()
            .WithMessage("Country code must be exactly 2 uppercase ASCII characters (Parameter 'str')");
    }

    [Test]
    [Arguments("US", 'U', 'S')]
    [Arguments("DE", 'D', 'E')]
    public async Task TryParseAndValidate_ValidCode_ShouldReturnTrue(string str, char char1, char char2)
    {
        // Act
        var result = Alpha2CountryCode.TryParse(str, out var c);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(c.Char1).IsEqualTo(char1);
        await Assert.That(c.Char2).IsEqualTo(char2);
    }

    [Test]
    [Arguments("E")]
    [Arguments("INVALID")]
    [Arguments("us")]
    [Arguments("Us")]
    [Arguments("uS")]
    [Arguments("12")]
    [Arguments("U1")]
    [Arguments("1U")]
    [Arguments("ÆØ")]
    [Arguments(":D")]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("  ")]
    [Arguments("       ")]
    public async Task TryParseAndValidate_InvalidCode_ShouldReturnFalse(string str)
    {
        // Act
        var result = Alpha2CountryCode.TryParse(str, out var c);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(c == Alpha2CountryCode.UnknownCountry).IsTrue();
    }

    [Test]
    [Arguments("US", "US", 0x5553_5553)]
    [Arguments("US", "DE", 0x4445_5553)]
    [Arguments("DE", "US", 0x4445_5553)]
    public async Task GetCombinedHashCode_ShouldReturnCombined(string str1, string str2, int expected)
    {
        // Act
        var result = Alpha2CountryCode.GetCombinedHashCode(str1, str2);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("US", 0x5553)]
    [Arguments("DE", 0x4445)]
    [Arguments("NO", 0x4E4F)]
    public async Task GetHashcode_ShouldReturnHash(string str, int expected)
    {
        // Arrange
        Alpha2CountryCode code = str;

        // Act
        var result = code.GetHashCode();

        // Assert
        await Assert.That(result).IsEqualTo(expected); // "US"
    }

    [Test]
    public async Task IsUnknown_ShouldReturnTrue()
    {
        // Arrange
        Alpha2CountryCode code = Alpha2CountryCode.UnknownCountry;

        // Act
        var result = code.IsUnknown();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UnknownCountry_IsEqualTo_XX()
    {
        // Arrange
        Alpha2CountryCode code = "XX";

        // Act
        var result = code == Alpha2CountryCode.UnknownCountry;

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    [Arguments("US")]
    [Arguments("DE")]
    [Arguments("NO")]
    public async Task IsUnknown_Known_ShouldReturnFalse(string str)
    {
        // Arrange
        Alpha2CountryCode code = str;

        // Act
        var result = code.IsUnknown();

        // Assert
        await Assert.That(result).IsFalse();
    }
}
