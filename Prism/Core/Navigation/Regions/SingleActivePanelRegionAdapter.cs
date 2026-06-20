using System.Linq;
using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class SingleActivePanelRegionAdapter : RegionAdapterBase<Panel>
{
    protected override void Activate(Panel regionTarget, object view)
    {
        var child = EnsureControl(view);
        foreach (var oldChild in regionTarget.Children.Where(existing => !ReferenceEquals(existing, child)).ToList())
        {
            regionTarget.Children.Remove(oldChild);
        }

        if (!regionTarget.Children.Contains(child))
        {
            regionTarget.Children.Add(child);
        }
    }

    protected override void Deactivate(Panel regionTarget, object view)
    {
        Remove(regionTarget, view);
    }

    protected override void Remove(Panel regionTarget, object view)
    {
        if (view is Control child)
        {
            regionTarget.Children.Remove(child);
        }
    }
}
