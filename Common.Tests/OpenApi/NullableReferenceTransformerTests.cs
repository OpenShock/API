using Microsoft.OpenApi;
using OpenShock.Common.OpenApi;

namespace OpenShock.Common.Tests.OpenApi;

public class NullableReferenceTransformerTests
{
    private static OpenApiDocument CreateDocument(IOpenApiSchema property) => new()
    {
        Info = new OpenApiInfo { Title = "test", Version = "1.0" },
        Components = new OpenApiComponents
        {
            Schemas = new Dictionary<string, IOpenApiSchema>
            {
                ["Child"] = new OpenApiSchema { Type = JsonSchemaType.Object },
                ["Parent"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema> { ["child"] = property },
                    Required = new HashSet<string> { "child" }
                }
            }
        }
    };

    /// <summary>What Microsoft.AspNetCore.OpenApi exports for a nullable reference property.</summary>
    private static OpenApiSchema NullableReference() => new()
    {
        OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, new OpenApiSchemaReference("Child")]
    };

    private static OpenApiSchema GetChildProperty(OpenApiDocument document) =>
        (OpenApiSchema)((OpenApiSchema)document.Components!.Schemas!["Parent"]).Properties!["child"];

    private static async Task TransformAsync(OpenApiDocument document) =>
        await new NullableReferenceTransformer().TransformAsync(document, null!, CancellationToken.None);

    [Test]
    public async Task NullableReference_BecomesNullableAllOf()
    {
        // Arrange
        var document = CreateDocument(NullableReference());

        // Act
        await TransformAsync(document);
        var property = GetChildProperty(document);

        // Assert
        await Assert.That(property.OneOf).IsNull();
        await Assert.That(property.Type).IsEqualTo(JsonSchemaType.Null);
        await Assert.That(property.AllOf?.OfType<OpenApiSchemaReference>().Single().Reference.Id).IsEqualTo("Child");
    }

    [Test]
    public async Task NullableReference_SerializesWithNullableNextToAllOf()
    {
        // Arrange
        var document = CreateDocument(NullableReference());

        // Act
        await TransformAsync(document);
        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);

        // Assert
        // Generated clients read this pair as a nullable reference; a bare $ref would type the property as
        // non-nullable and a oneOf as a union with an unknown member.
        await Assert.That(json).Contains("\"nullable\": true");
        await Assert.That(json).Contains("\"allOf\"");
        await Assert.That(json).DoesNotContain("\"oneOf\"");
    }

    [Test]
    public async Task NullEnumMember_IsRecognisedAsNull()
    {
        // Arrange
        // 3.0 has no `type: null`, so a downgraded null member can arrive as a single-value enum instead.
        var document = CreateDocument(new OpenApiSchema
        {
            OneOf = [new OpenApiSchema { Enum = [null!] }, new OpenApiSchemaReference("Child")]
        });

        // Act
        await TransformAsync(document);
        var property = GetChildProperty(document);

        // Assert
        await Assert.That(property.OneOf).IsNull();
        await Assert.That(property.Type).IsEqualTo(JsonSchemaType.Null);
    }

    [Test]
    public async Task DescribedWrapper_IsLeftAlone()
    {
        // Arrange
        var wrapper = NullableReference();
        wrapper.Description = "A union that means something of its own.";
        var document = CreateDocument(wrapper);

        // Act
        await TransformAsync(document);
        var property = GetChildProperty(document);

        // Assert
        await Assert.That(property.OneOf?.Count).IsEqualTo(2);
        await Assert.That(property.AllOf).IsNull();
    }

    [Test]
    public async Task NonNullableReference_IsLeftAlone()
    {
        // Arrange
        var document = CreateDocument(new OpenApiSchemaReference("Child"));

        // Act
        await TransformAsync(document);
        var property = ((OpenApiSchema)document.Components!.Schemas!["Parent"]).Properties!["child"];

        // Assert
        await Assert.That(property).IsTypeOf<OpenApiSchemaReference>();
    }

    [Test]
    public async Task UnionOfTwoReferences_IsLeftAlone()
    {
        // Arrange
        var document = CreateDocument(new OpenApiSchema
        {
            OneOf = [new OpenApiSchemaReference("Child"), new OpenApiSchemaReference("Parent")]
        });

        // Act
        await TransformAsync(document);
        var property = GetChildProperty(document);

        // Assert
        await Assert.That(property.OneOf?.Count).IsEqualTo(2);
        await Assert.That(property.AllOf).IsNull();
    }
}
