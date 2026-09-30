using Microsoft.OpenApi;

namespace OpenShock.Common.DataAnnotations.Interfaces;

/// <summary>
/// Represents an interface for parameter attributes that can be applied to an OpenApiSchema or OpenApiParameter instance.
/// </summary>
public interface IParameterAttribute
{
    /// <summary>
    /// Applies the parameter attribute to the given OpenApiSchema instance.
    /// </summary>
    /// <param name="schema">The OpenApiSchema instance to apply the attribute to.</param>
    void Apply(OpenApiSchema schema);

    /// <summary>
    /// Applies the parameter attribute to the given OpenApiParameter instance.
    /// </summary>
    /// <param name="parameter">The OpenApiParameter instance to apply the attribute to.</param>
    void Apply(OpenApiParameter parameter);
}