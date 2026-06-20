using System.Text.RegularExpressions;

namespace Prism.Navigation.Regions;

internal sealed class NavigationRouteRegistration
{
    public NavigationRouteRegistration(
        string routeTemplate,
        string target,
        Regex regex,
        IReadOnlyList<string> parameterNames,
        IReadOnlyDictionary<string, object?> defaultValues,
        Func<INavigationParameters, bool>? constraint)
    {
        RouteTemplate = routeTemplate;
        Target = target;
        Regex = regex;
        ParameterNames = parameterNames;
        DefaultValues = defaultValues;
        Constraint = constraint;
    }

    public string RouteTemplate { get; }

    public string Target { get; }

    public Regex Regex { get; }

    public IReadOnlyList<string> ParameterNames { get; }

    public IReadOnlyDictionary<string, object?> DefaultValues { get; }

    public Func<INavigationParameters, bool>? Constraint { get; }
}