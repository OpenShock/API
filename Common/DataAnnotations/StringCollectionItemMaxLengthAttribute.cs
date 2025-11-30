using System.ComponentModel.DataAnnotations;

namespace OpenShock.Common.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StringCollectionItemMaxLengthAttribute : ValidationAttribute
{
    public StringCollectionItemMaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }

    public int MaxLength { get; }

    public override bool IsValid(object? value)
    {
        return value is IEnumerable<string> items && items.All(item => item.Length <= MaxLength);
    }
}