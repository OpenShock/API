using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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
        return GetAllControllers(assembly).SelectMany(type => type.GetCustomAttributes<TAttribute>(true).Concat(type.GetMethods(BindingFlags.Instance).SelectMany(m => m.GetCustomAttributes<TAttribute>())));
    }
}
