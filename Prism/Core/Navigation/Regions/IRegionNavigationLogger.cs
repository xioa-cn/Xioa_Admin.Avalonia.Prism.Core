using System;

namespace Prism.Navigation.Regions;

public interface IRegionNavigationLogger
{
    void NavigationStarting(NavigationContext navigationContext);

    void NavigationSucceeded(NavigationContext navigationContext, object view);

    void NavigationCanceled(NavigationContext navigationContext);

    void NavigationFailed(NavigationContext navigationContext, Exception exception);

    void ViewDestroyed(object view);
}