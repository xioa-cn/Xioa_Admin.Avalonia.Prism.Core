using Prism.Ioc;

namespace Prism.Navigation.Regions;

public sealed class ContainerRegionViewFactory : IRegionViewFactory
{
    private readonly IContainerProvider _container;

    public ContainerRegionViewFactory(IContainerProvider container)
    {
        _container = container;
    }

    public object CreateView(Type viewType) => _container.Resolve(viewType);

    public object CreateView(string viewName) => _container.Resolve(typeof(object), viewName);
}
