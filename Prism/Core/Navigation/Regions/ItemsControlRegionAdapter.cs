using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Prism.Navigation.Regions;

public sealed class ItemsControlRegionAdapter : RegionAdapterBase<ItemsControl>
{
    private static readonly ConditionalWeakTable<ItemsControl, ObservableCollection<object>> ItemsCache = new();

    protected override void Add(ItemsControl regionTarget, object view)
    {
        var items = GetWritableItems(regionTarget);
        if (!items.Contains(view))
        {
            items.Add(view);
        }
    }

    protected override void Remove(ItemsControl regionTarget, object view)
    {
        var items = GetWritableItems(regionTarget);
        items.Remove(view);
    }

    private static IList GetWritableItems(ItemsControl regionTarget)
    {
        if (regionTarget.ItemsSource is IList existingItems)
        {
            return existingItems;
        }

        if (regionTarget.ItemsSource is not null)
        {
            throw new InvalidOperationException($"ItemsControl region '{regionTarget.GetType().FullName}' has a read-only ItemsSource. Use an IList/ObservableCollection ItemsSource or leave ItemsSource empty.");
        }

        return ItemsCache.GetValue(regionTarget, control =>
        {
            var items = new ObservableCollection<object>();
            control.ItemsSource = items;
            return items;
        });
    }
}
