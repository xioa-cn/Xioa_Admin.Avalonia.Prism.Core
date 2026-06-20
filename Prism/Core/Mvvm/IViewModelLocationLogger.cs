using System;

namespace Prism.Mvvm;

public interface IViewModelLocationLogger
{
    void ResolutionSucceeded(Type viewType, Type viewModelType, string matchingRule);

    void ResolutionFailed(Type viewType, Exception exception);

    void CacheHit(Type viewType, Type viewModelType, string cacheRule);

    void CacheEvicted(string cacheKey);
}
