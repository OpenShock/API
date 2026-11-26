using OpenShock.Common.OpenShockDb.Converters;

namespace OpenShock.Common.Tests.OpenShockDb;

public sealed class SaturatingConverterTests
{
    [Test]
    [Arguments(0, (ushort)0)]
    [Arguments(65535, (ushort)65535)]
    [Arguments(65536, (ushort)65535)]   // an unchecked cast would read 0
    [Arguments(100000, (ushort)65535)]  // an unchecked cast would read 34464
    [Arguments(-1, (ushort)0)]          // an unchecked cast would read 65535
    public async Task UInt16_ClampsOnRead(int stored, ushort expected)
    {
        await Assert.That(new SaturatingUInt16Converter().ConvertFromProvider(stored)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0L, 0u)]
    [Arguments(4294967295L, 4294967295u)]
    [Arguments(4294967296L, 4294967295u)]
    [Arguments(-1L, 0u)]
    public async Task UInt32_ClampsOnRead(long stored, uint expected)
    {
        await Assert.That(new SaturatingUInt32Converter().ConvertFromProvider(stored)).IsEqualTo(expected);
    }

    [Test]
    public async Task ValuesInRange_RoundTrip()
    {
        await Assert.That(new SaturatingUInt16Converter().ConvertToProvider(ushort.MaxValue)).IsEqualTo(65535);
        await Assert.That(new SaturatingUInt32Converter().ConvertToProvider(uint.MaxValue)).IsEqualTo(4294967295L);
    }
}
