using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public interface IRegionAdapter
{
    IRegion Initialize(Control regionTarget, string regionName, IRegionManager regionManager);

    void AddView(Control regionTarget, object view);

    void ActivateView(Control regionTarget, object view);

    void DeactivateView(Control regionTarget, object view);

    void RemoveView(Control regionTarget, object view);
}