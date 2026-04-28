using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class CryptoUtilsTests
{
    [Test]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(64)]
    [Arguments(256)]
    public async Task RandomAlphaNumericString_CorrectLength(int length)
    {
        var result = CryptoUtils.RandomAlphaNumericString(length);
        await Assert.That(result.Length).IsEqualTo(length);
    }

    [Test]
    public async Task RandomAlphaNumericString_OnlyAlphaNumericChars()
    {
        var result = CryptoUtils.RandomAlphaNumericString(1000);
        await Assert.That(result.All(c => char.IsLetterOrDigit(c))).IsTrue();
    }

    [Test]
    public async Task RandomAlphaNumericString_ContainsVariety()
    {
        // With 1000 chars, should have both letters and digits
        var result = CryptoUtils.RandomAlphaNumericString(1000);
        await Assert.That(result.Any(char.IsLetter)).IsTrue();
        await Assert.That(result.Any(char.IsDigit)).IsTrue();
    }

    [Test]
    public async Task RandomAlphaNumericString_TwoCallsProduceDifferentResults()
    {
        var a = CryptoUtils.RandomAlphaNumericString(32);
        var b = CryptoUtils.RandomAlphaNumericString(32);
        await Assert.That(a).IsNotEqualTo(b);
    }

    [Test]
    [Arguments(1)]
    [Arguments(6)]
    [Arguments(10)]
    [Arguments(100)]
    public async Task RandomNumericString_CorrectLength(int length)
    {
        var result = CryptoUtils.RandomNumericString(length);
        await Assert.That(result.Length).IsEqualTo(length);
    }

    [Test]
    public async Task RandomNumericString_OnlyDigits()
    {
        var result = CryptoUtils.RandomNumericString(1000);
        await Assert.That(result.All(char.IsDigit)).IsTrue();
    }

    [Test]
    public async Task RandomNumericString_TwoCallsProduceDifferentResults()
    {
        var a = CryptoUtils.RandomNumericString(32);
        var b = CryptoUtils.RandomNumericString(32);
        await Assert.That(a).IsNotEqualTo(b);
    }
}
