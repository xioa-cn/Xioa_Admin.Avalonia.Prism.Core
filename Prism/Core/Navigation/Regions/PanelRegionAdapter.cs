using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class PanelRegionAdapter : RegionAdapterBase<Panel>
{
    protected override void Add(Panel regionTarget, object view)
    {
        var child = EnsureControl(view);
        if (!regionTarget.Children.Contains(child))
        {
            regionTarget.Children.Add(child);
        }
    }

    protected override void Remove(Panel regionTarget, object view)
    {
        if (view is Control child)
        {
            regionTarget.Children.Remove(child);
        }
    }
}