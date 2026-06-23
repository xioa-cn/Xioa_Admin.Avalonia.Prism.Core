using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Converter;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Prism.Events;
using SukiUI.Controls;

namespace Ava.Xioa.InfrastructureModule.Components;

public partial class NavigableMenu : UserControl, INotifyPropertyChanged, INotifyPropertyChanging
{
    [ObservableBindProperty] private bool _isMenuExpanded = true;

    public NavigableMenu()
    {
        this.OnceExecutedLoaded(OnLoaded);
        InitializeComponent();
    }

    partial void OnIsMenuExpandedChanged(bool value)
    {
        if (value) return;

        if (_sukiSideMenu is null) return;

        foreach (var item in _sukiSideMenu.Items)
        {
            if (item is SukiSideMenuItem sideMenuItem)
            {
                sideMenuItem.IsExpanded = false;
            }
        }
    }

    private SukiSideMenu? _sukiSideMenu;
    private SukiSideMenuItem? _lastSelectedLeafItem;
    private bool _isRestoringSelection;

    private void OnLoaded()
    {
        if (this.DataContext is not IHomeServices homeServices) return;
        var sukiSideMenu =
            SukiSideMenuHelper.GetNavigableMenu(homeServices.NavigableMenuServices.NavigableMenuItems);

        sukiSideMenu.Content = new NavigableMenuContent();
        sukiSideMenu.DataContext = homeServices.MainWindowServices;
        var image = new Image();
        image.Width = 40;
        image.Height = 40;
        image.Stretch = Stretch.UniformToFill;
        image.Margin = new Thickness(30, 10, 30, 30);
        image.Classes.Add("AppIcon");
        image.HorizontalAlignment = HorizontalAlignment.Center;
        image.Bind(Image.SourceProperty, new Binding("Icon")
        {
            Source = homeServices.MainWindowServices,
            Converter = new WindowIconToImageSourceConverter()
        });
        image.PointerPressed += (s, e) =>
        {
            homeServices.MainWindowServices.IsMenuVisible = !homeServices.MainWindowServices.IsMenuVisible;
        };
        sukiSideMenu.HeaderContent = image;

        // sukiSideMenu.Bind(SukiSideMenu.SelectedItemProperty,
        //     new Binding("SelectedView") { Source = homeServices.NavigableMenuServices });

        sukiSideMenu.Bind(SukiSideMenu.IsMenuExpandedProperty,
            new Binding("IsMenuExpanded") { Source = this, Mode = BindingMode.TwoWay });

        sukiSideMenu.SelectionChanged += SukiSideMenuOnSelectionChanged;
        _sukiSideMenu = sukiSideMenu;
        GlobalEventAggregator.EventAggregator?.GetEvent<NavigableReverseSelectionEvent>()
            .Subscribe(OnReverseSelection, ThreadOption.UIThread, true,
                filter => filter.TokenKey == "ReverseSelection");
        this.ContentControl.Content = sukiSideMenu;
    }

    private void OnReverseSelection(TokenKeyPubSubEvent<ReverseSelectionPub> obj)
    {
        if (_sukiSideMenu is null)
        {
            return;
        }

        _lastSelectedLeafItem = FindMenuItemByTag(_sukiSideMenu.Items, obj.Value.Key);
    }

    private void SukiSideMenuOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringSelection)
        {
            return;
        }

        if (sender is SukiSideMenu sukiSideMenu && sukiSideMenu.SelectedItem is SukiSideMenuItem item)
        {
            if (item.ItemCount > 0)
            {
                item.IsExpanded = !item.IsExpanded;
            }

            if (!sukiSideMenu.IsMenuExpanded && item.IsExpanded)
            {
                item.IsTopMenuExpanded = true;
                sukiSideMenu.IsMenuExpanded = true;
            }

            if (this.DataContext is IHomeServices homeServices && item.ItemCount == 0)
            {
                if (item.Tag is not string key)
                {
                    return;
                }

                if (PoppedNavigationWindowRegistry.TryActivate(key))
                {
                    RestoreLastSelectedItem(sukiSideMenu);
                    return;
                }

                _lastSelectedLeafItem = item;
                homeServices.NavigableMenuServices.SelectedView = key;
            }
        }
    }

    private void RestoreLastSelectedItem(SukiSideMenu sukiSideMenu)
    {
        if (_lastSelectedLeafItem is null)
        {
            return;
        }

        _isRestoringSelection = true;
        sukiSideMenu.SelectedItem = _lastSelectedLeafItem;
        _lastSelectedLeafItem.IsSelected = true;
        _isRestoringSelection = false;
    }

    private static SukiSideMenuItem? FindMenuItemByTag(IEnumerable<object?> items, string key)
    {
        foreach (var sourceItem in items)
        {
            if (sourceItem is not SukiSideMenuItem item)
            {
                continue;
            }

            if (item.ItemCount == 0 && item.Tag is string tag && tag == key)
            {
                return item;
            }

            var child = FindMenuItemByTag(item.Items, key);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
