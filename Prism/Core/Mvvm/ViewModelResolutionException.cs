using System;
using System.Collections.Generic;

namespace Prism.Mvvm;

public sealed class ViewModelResolutionException : InvalidOperationException
{
    public ViewModelResolutionException(
        Type viewType,
        string? routeName,
        string? moduleName,
        IReadOnlyList<string> candidatePaths,
        IReadOnlyList<string> searchedAssemblies,
        string matchingRule,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ViewType = viewType;
        RouteName = routeName;
        ModuleName = moduleName;
        CandidatePaths = candidatePaths;
        SearchedAssemblies = searchedAssemblies;
        MatchingRule = matchingRule;
    }

    public Type ViewType { get; }

    public string? RouteName { get; }

    public string? ModuleName { get; }

    public IReadOnlyList<string> CandidatePaths { get; }

    public IReadOnlyList<string> SearchedAssemblies { get; }

    public string MatchingRule { get; }
}