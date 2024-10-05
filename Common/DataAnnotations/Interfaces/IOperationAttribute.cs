using Microsoft.OpenApi.Models;

namespace OpenShock.Common.DataAnnotations.Interfaces;

/// <summary>
/// Represents an interface for operation attributes that can be applied to an OpenApiOperation instance.
/// </summary>
public interface IOperationAttribute
{
    /// <summary>
    /// Applies the operation attribute to the given OpenApiOperation instance.
    /// </summary>
    /// <param name="operation">The OpenApiOperation instance to apply the attribute to.</param>
    void Apply(OpenApiOperation operation);
}