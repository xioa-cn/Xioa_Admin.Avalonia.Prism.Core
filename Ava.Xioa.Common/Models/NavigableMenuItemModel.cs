using System;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Utils;
using Material.Icons;
using Prism.Events;

namespace Ava.Xioa.Common.Models;

public class ResourcesRouters
{
    public NavigableMenuItemModel[] Routers { get; set; }
}

public class NavigableMenuItemModel : ReactiveObject
{
    public NavigableMenuItemModel(string key) : this()
    {
        Key = key;
    }

    public NavigableMenuItemModel()
    {
        if (GlobalEventAggregator.EventAggregator is null)
        {
            throw new ArgumentNullException();
        }

        GlobalEventAggregator.EventAggregator.GetEvent<NavigableReverseSelectionEvent>()
            .Subscribe(
                ReverseSelection
                , ThreadOption.UIThread, true,
                filter =>
                    filter.TokenKey == "ReverseSelection"
            );
    }

    private void ReverseSelection(TokenKeyPubSubEvent<ReverseSelectionPub> obj)
    {
        this.IsSelected = obj.Value.Key == Key;
    }

    public string Header { get; set; }

    public string Key { get; set; }

    public MaterialIconKind IconKind { get; set; }

    public NavigableMenuItemModel[]? Children { get; set; }

    public bool HasChildren => Children is not null && Children.Length > 0;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.SetProperty(ref _isSelected, value);
    }

    public string NavigationName { get; set; }

    public string Region { get; set; }
}