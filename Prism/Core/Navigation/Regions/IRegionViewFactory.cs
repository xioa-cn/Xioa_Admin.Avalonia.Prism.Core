using System;

namespace Prism.Navigation.Regions;

public interface IRegionViewFactory
{
    object CreateView(Type viewType);

    object CreateView(string viewName);
}
