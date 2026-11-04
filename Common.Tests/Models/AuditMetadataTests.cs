using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenShock.Common.Models;

namespace OpenShock.Common.Tests.Models;

/// <summary>
/// <see cref="AuditMetadata"/> is closed, so the compiler keeps every subtype in this assembly, but it can't check that
/// each one is also registered for polymorphic JSON. An unregistered subtype only fails once it gets serialized.
/// </summary>
public class AuditMetadataTests
{
    private static readonly Type[] Subtypes = typeof(AuditMetadata).Assembly.GetTypes()
        .Where(t => t != typeof(AuditMetadata) && typeof(AuditMetadata).IsAssignableFrom(t))
        .ToArray();

    private static readonly JsonDerivedTypeAttribute[] Registrations =
        typeof(AuditMetadata).GetCustomAttributes<JsonDerivedTypeAttribute>().ToArray();

    [Test]
    public async Task EverySubtype_IsRegisteredForJson()
    {
        var registered = Registrations.Select(r => r.DerivedType).ToHashSet();
        var unregistered = Subtypes.Where(t => !registered.Contains(t)).Select(t => t.Name).ToArray();

        await Assert.That(Subtypes).IsNotEmpty();
        await Assert.That(unregistered).IsEmpty();
    }

    [Test]
    public async Task Discriminators_AreUnique()
    {
        var duplicates = Registrations.GroupBy(r => r.TypeDiscriminator).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();

        await Assert.That(duplicates).IsEmpty();
    }

    [Test]
    public async Task Metadata_RoundTripsThroughTheBaseType()
    {
        // Same call shape as the EF value converter in OpenShockContext
        AuditMetadata metadata = new UsernameChangedMetadata("old", "new");

        var json = JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null);
        var back = JsonSerializer.Deserialize<AuditMetadata>(json, (JsonSerializerOptions?)null);

        await Assert.That(json).Contains("\"t\":\"usernameChanged\"");
        await Assert.That(back).IsEqualTo(metadata);
    }
}
