using Microsoft.OpenApi.Models;
using ShockLink.API.DataAnnotations.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ShockLink.API.Utils;

public sealed class AttributeFilter : ISchemaFilter, IParameterFilter, IOperationFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        foreach (IParameterAttribute attribute in context.ParameterInfo?.GetCustomAttributes(true)?.OfType<IParameterAttribute>() ?? Enumerable.Empty<IParameterAttribute>())
        {
            attribute.Apply(parameter);
        }
        foreach (IParameterAttribute attribute in context.PropertyInfo?.GetCustomAttributes(true)?.OfType<IParameterAttribute>() ?? Enumerable.Empty<IParameterAttribute>())
        {
            attribute.Apply(parameter);
        }
    }
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (IParameterAttribute attribute in context.MemberInfo?.GetCustomAttributes(true)?.OfType<IParameterAttribute>() ?? Enumerable.Empty<IParameterAttribute>())
        {
            attribute.Apply(schema);
        }
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (IOperationAttribute attribute in context.MethodInfo?.GetCustomAttributes(true)?.OfType<IOperationAttribute>() ?? Enumerable.Empty<IOperationAttribute>())
        {
            attribute.Apply(operation);
        }
    }
}