using System.Text.RegularExpressions;

namespace Prism.Navigation.Regions;

internal sealed class NavigationRoutePattern
{
    public NavigationRoutePattern(Regex regex, IReadOnlyList<string> parameterNames, IReadOnlyDictionary<string, object?> defaultValues)
    {
        Regex = regex;
        ParameterNames = parameterNames;
        DefaultValues = defaultValues;
    }

    public Regex Regex { get; }

    public IReadOnlyList<string> ParameterNames { get; }

    public IReadOnlyDictionary<string, object?> DefaultValues { get; }
}