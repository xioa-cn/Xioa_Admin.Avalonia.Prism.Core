namespace Prism.Navigation.Regions;

public interface INavigationInterceptor
{
    Task<bool> CanNavigateAsync(NavigationContext navigationContext);
}
