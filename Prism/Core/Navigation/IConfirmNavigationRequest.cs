using System;
using System.Threading.Tasks;

namespace Prism.Navigation;

public interface IConfirmNavigationRequest : INavigationAware
{
    void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback);
}

public interface IConfirmNavigationRequestAsync : INavigationAware
{
    Task<bool> ConfirmNavigationRequestAsync(NavigationContext navigationContext);
}