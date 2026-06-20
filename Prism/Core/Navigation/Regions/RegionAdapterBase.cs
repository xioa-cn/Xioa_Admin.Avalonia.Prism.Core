using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public abstract class RegionAdapterBase<TControl> : IRegionAdapter
    where TControl : Control
{
    public IRegion Initialize(Control regionTarget, string regionName, IRegionManager regionManager)
    {
        var typedTarget = EnsureTarget(regionTarget);
        var region = CreateRegion();
        region.Name = regionName;
        region.RegionManager = regionManager;
        InitializeRegion(typedTarget, region);
        return region;
    }

    public void AddView(Control regionTarget, object view) => Add(EnsureTarget(regionTarget), view);

    public void ActivateView(Control regionTarget, object view) => Activate(EnsureTarget(regionTarget), view);

    public void DeactivateView(Control regionTarget, object view) => Deactivate(EnsureTarget(regionTarget), view);

    public void RemoveView(Control regionTarget, object view) => Remove(EnsureTarget(regionTarget), view);

    protected virtual IRegion CreateRegion() => new Region();

    protected virtual void InitializeRegion(TControl regionTarget, IRegion region)
    {
    }

    protected virtual void Add(TControl regionTarget, object view)
    {
    }

    protected virtual void Activate(TControl regionTarget, object view)
    {
    }

    protected virtual void Deactivate(TControl regionTarget, object view)
    {
    }

    protected virtual void Remove(TControl regionTarget, object view)
    {
    }

    protected static Control EnsureControl(object view)
    {
        return view as Control ?? throw new InvalidOperationException($"Region view must be an Avalonia control. View type: {view.GetType().FullName}.");
    }

    private TControl EnsureTarget(Control regionTarget)
    {
        return regionTarget is TControl typedTarget
            ? typedTarget
            : throw new InvalidOperationException($"Region adapter {GetType().FullName} cannot adapt {regionTarget.GetType().FullName}.");
    }
}