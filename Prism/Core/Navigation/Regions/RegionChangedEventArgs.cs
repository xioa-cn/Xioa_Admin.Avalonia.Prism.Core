using System;
using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class RegionChangedEventArgs : EventArgs
{
    public RegionChangedEventArgs(IRegion region, Control regionTarget)
    {
        Region = region;
        RegionTarget = regionTarget;
    }

    public IRegion Region { get; }

    public Control RegionTarget { get; }
}