using System;

namespace Prism.Mvvm;

public sealed class NullViewModelLocationLogger : IViewModelLocationLogger
{
    public static readonly NullViewModelLocationLogger Instance = new();

    private NullViewModelLocationLogger()
    {
    }

    public void ResolutionSucceeded(Type viewType, Type viewModelType, string matchingRule)
    {
    }

    public void ResolutionFailed(Type viewType, Exception exception)
    {
    }

    public void CacheHit(Type viewType, Type viewModelType, string cacheRule)
    {
    }

    public void CacheEvicted(string cacheKey)
    {
    }
}