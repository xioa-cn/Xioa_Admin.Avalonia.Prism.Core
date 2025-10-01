using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Themes.Converter;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SukiUI.Controls;

namespace Ava.Xioa.InfrastructureModule.Components;

public partial class NavigableMenu : UserControl, INotifyPropertyChanged, INotifyPropertyChanging
{
    [ObservableBindProperty] private bool _isMenuExpanded = true;

    public NavigableMenu()
    {
        this.Loaded += OnLoaded;
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

    private void OnLoaded(object? sender, RoutedEventArgs e)
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
        this.ContentControl.Content = sukiSideMenu;
    }

    private void SukiSideMenuOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
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
                homeServices.NavigableMenuServices.SelectedView = item.Tag as string;
            }
        }
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