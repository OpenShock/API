using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace OpenShock.Common.Extensions;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetAllControllers(this Assembly assembly)
    {
        return assembly
                .GetTypes()
                .Where(type => type.IsClass && typeof(ControllerBase).IsAssignableFrom(type));
    }

    public static IEnumerable<TAttribute> GetAllControllerEndpointAttributes<TAttribute>(this Assembly assembly) where TAttribute : Attribute
    {
        return assembly.GetAllControllers().SelectMany(type => type.GetCustomAttributes<TAttribute>(true).Concat(type.GetMethods(BindingFlags.Instance).SelectMany(m => m.GetCustomAttributes<TAttribute>())));
    }
}
