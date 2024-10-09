using OpenShock.Common.Validation;

namespace OpenShock.Common.Tests.Validation;

internal class CharsetMatchersTests
{
    private readonly string whitelist;
    private readonly string[] blacklist;

    public CharsetMatchersTests()
    {
        whitelist = File.ReadAllText("Validation/DataSets/WhiteList.txt");
        blacklist = File.ReadAllLines("Validation/DataSets/BlackList.txt");
    }

    [Test]
    public async Task ContainsUnwanted_Whitelist_ReturnsFalse()
    {
        // Act
        bool result = CharsetMatchers.ContainsUnwantedUserInterfaceRunes(whitelist);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ContainsUnwanted_BlackList_AllReturnTrue()
    {
        foreach (var line in blacklist)
        {
            // Skip empty lines
            if (string.IsNullOrEmpty(line)) continue;

            // Act
            bool result = CharsetMatchers.ContainsUnwantedUserInterfaceRunes(line);

            // Assert
            await Assert.That(result).IsTrue();
        }
    }
}
