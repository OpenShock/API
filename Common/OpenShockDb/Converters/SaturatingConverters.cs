using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OpenShock.Common.OpenShockDb.Converters;

// Postgres has no unsigned integers, so an unsigned property is stored in the next wider signed column. EF's
// default for that is a plain unchecked cast, which keeps only the low bits of a stored value the property
// can't hold: 100000 in an integer column reads back as 34464. These converters saturate instead, so a value
// that is out of range reads as the nearest one the type can hold.

/// <summary>
/// Stores a <see cref="ushort"/> in an integer column, clamping on read instead of wrapping.
/// </summary>
public sealed class SaturatingUInt16Converter() : ValueConverter<ushort, int>(
    value => value,
    stored => (ushort)Math.Clamp(stored, ushort.MinValue, ushort.MaxValue));

/// <summary>
/// Stores a <see cref="uint"/> in a bigint column, clamping on read instead of wrapping.
/// </summary>
public sealed class SaturatingUInt32Converter() : ValueConverter<uint, long>(
    value => value,
    stored => (uint)Math.Clamp(stored, uint.MinValue, uint.MaxValue));
