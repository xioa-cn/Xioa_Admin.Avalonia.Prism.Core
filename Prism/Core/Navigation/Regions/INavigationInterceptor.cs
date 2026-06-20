using System.Threading.Tasks;

namespace Prism.Navigation.Regions;

public interface INavigationInterceptor
{
    Task<bool> CanNavigateAsync(NavigationContext navigationContext);
}
