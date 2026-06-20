using Prism.Ioc;

namespace Prism.Modularity;

public interface IModule
{
    void RegisterTypes(IContainerRegistry containerRegistry);

    void OnInitialized(IContainerProvider containerProvider);
}