using OpenShock.Common.Validation;

namespace OpenShock.Common.Tests.Validation;

internal class CharsetMatchersTests
{
    private readonly string[] _whitelist = File.ReadAllLines("Validation/DataSets/WhiteList.txt");
    private readonly string[] _blacklist = File.ReadAllLines("Validation/DataSets/BlackList.txt");

    [Test]
    public async Task ContainsUnwanted_Whitelist_ReturnsFalse()
    {
        foreach (var line in _whitelist)
        {
            // Skip empty lines
            if (string.IsNullOrEmpty(line)) continue;

            // Act
            bool result = CharsetMatchers.ContainsUndesiredUserInterfaceCharacters(line);

            // Assert
            await Assert.That(result).IsFalse();
        }
    }

    [Test]
    public async Task ContainsUnwanted_BlackList_AllReturnTrue()
    {
        foreach (var line in _blacklist)
        {
            // Skip empty lines
            if (string.IsNullOrEmpty(line)) continue;

            // Act
            bool result = CharsetMatchers.ContainsUndesiredUserInterfaceCharacters(line);

            // Assert
            await Assert.That(result).IsTrue();
        }
    }
}
