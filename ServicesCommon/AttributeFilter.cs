using Microsoft.OpenApi.Models;
using OpenShock.ServicesCommon.DataAnnotations.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpenShock.ServicesCommon
{
    public sealed class AttributeFilter : ISchemaFilter, IParameterFilter, IOperationFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            foreach (var attribute in context.ParameterInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ??
                                      Enumerable.Empty<IParameterAttribute>())
                attribute.Apply(parameter);
            foreach (var attribute in context.PropertyInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ??
                                      Enumerable.Empty<IParameterAttribute>())
                attribute.Apply(parameter);
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            foreach (var attribute in context.MemberInfo?.GetCustomAttributes(true).OfType<IParameterAttribute>() ??
                                      Enumerable.Empty<IParameterAttribute>()) attribute.Apply(schema);
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var attribute in context.MethodInfo?.GetCustomAttributes(true).OfType<IOperationAttribute>() ??
                                      Enumerable.Empty<IOperationAttribute>())
                attribute.Apply(operation);
        }
    }
}