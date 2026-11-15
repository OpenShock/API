using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace OpenShock.Common.ApiVersioning;

/// <summary>
/// Fills the <c>{version:apiVersion}</c> placeholder in the full route (controller route included) of every action with
/// <see cref="ApiVersionAttribute"/>, taken from the action method or else its controller: <c>/{version:apiVersion}/account</c> + <c>login</c> with <c>[ApiVersion(1)]</c> becomes <c>/1/account/login</c>.
/// </summary>
/// <remarks>
/// The placeholder and the attribute go together: a versioned action whose route has no placeholder, or a placeholder on an action
/// without a version, fails at startup.
/// </remarks>
public sealed class ApiVersionRouteConvention : IApplicationModelConvention
{
    public const string Placeholder = "{version:apiVersion}";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var controllerRoutes = controller.Selectors.Select(s => s.AttributeRouteModel).OfType<AttributeRouteModel>().ToArray();

            foreach (var action in controller.Actions)
            {
                var version = (action.ActionMethod.GetCustomAttribute<ApiVersionAttribute>(inherit: true)
                               ?? controller.ControllerType.GetCustomAttribute<ApiVersionAttribute>(inherit: true))?.Version;

                var selectors = action.Selectors.SelectMany(selector => Version(action, selector, controllerRoutes, version)).ToArray();

                action.Selectors.Clear();
                foreach (var selector in selectors) action.Selectors.Add(selector);
            }
        }
    }

    private static IEnumerable<SelectorModel> Version(ActionModel action, SelectorModel selector, AttributeRouteModel[] controllerRoutes, uint? version)
    {
        // The route the action ends up with: its own when absolute (or when the controller has none), otherwise combined with the controller's
        var routes = selector.AttributeRouteModel is { IsAbsoluteTemplate: true } || controllerRoutes.Length == 0
            ? [selector.AttributeRouteModel]
            : controllerRoutes.Select(controllerRoute => AttributeRouteModel.CombineAttributeRouteModel(controllerRoute, selector.AttributeRouteModel));

        if (version is null)
        {
            if (routes.Any(route => HasPlaceholder(route?.Template)))
                throw new InvalidOperationException($"{action.DisplayName} has {Placeholder} in its route but no {nameof(ApiVersionAttribute)}");

            yield return selector;
            yield break;
        }

        foreach (var route in routes)
        {
            if (route is null || !HasPlaceholder(route.Template))
                throw new InvalidOperationException($"{action.DisplayName} has an {nameof(ApiVersionAttribute)} but no {Placeholder} in its route");

            // Absolute, so it is no longer combined with the controller's route
            yield return new SelectorModel(selector)
            {
                AttributeRouteModel = new AttributeRouteModel(route)
                {
                    Template = "/" + route.Template!.TrimStart('~', '/').Replace(Placeholder, version.Value.ToString(), StringComparison.OrdinalIgnoreCase)
                }
            };
        }
    }

    private static bool HasPlaceholder(string? template) => template?.Contains(Placeholder, StringComparison.OrdinalIgnoreCase) ?? false;
}
