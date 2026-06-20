using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class ContentControlRegionAdapter : RegionAdapterBase<ContentControl>
{
    protected override void Add(ContentControl regionTarget, object view)
    {
        regionTarget.Content = view;
    }

    protected override void Activate(ContentControl regionTarget, object view)
    {
        regionTarget.Content = view;
    }

    protected override void Deactivate(ContentControl regionTarget, object view)
    {
        if (ReferenceEquals(regionTarget.Content, view))
        {
            regionTarget.Content = null;
        }
    }

    protected override void Remove(ContentControl regionTarget, object view)
    {
        Deactivate(regionTarget, view);
    }
}