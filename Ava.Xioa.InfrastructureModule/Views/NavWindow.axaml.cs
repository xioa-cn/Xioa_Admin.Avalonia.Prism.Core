using System;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

public partial class NavWindow : SukiWindow
{
    private Action<NavigableBarInfoModel, object>? _restoreToMain;
    private NavigableBarInfoModel? _navigationInfo;
    private object? _contentView;
    private bool _isRestored;

    public NavWindow()
    {
        InitializeComponent();
        RegionName = $"NavWindowRegion_{Guid.NewGuid():N}";
    }

    public NavWindow(
        IRegionManager regionManager,
        IThemesServices themesServices,
        NavigableBarInfoModel navigationInfo,
        object content,
        Action<NavigableBarInfoModel, object> restoreToMain)
        : this()
    {
        DataContext = themesServices;
        _navigationInfo = navigationInfo;
        _contentView = content;
        _restoreToMain = restoreToMain;

        Title = navigationInfo.Name;
        RegionManager.SetRegionManager(ContentHost, regionManager);
        RegionManager.SetRegionName(ContentHost, RegionName);
    }

    public string RegionName { get; }

    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (!IsVisible)
        {
            Show();
        }

        Activate();
    }

    private void RestoreToMainClick(object? sender, RoutedEventArgs e)
    {
        RestoreToMain();
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        RestoreToMain();
        base.OnClosing(e);
    }

    private void RestoreToMain()
    {
        if (_isRestored)
        {
            return;
        }

        if (_restoreToMain is null || _navigationInfo is null || _contentView is null)
        {
            return;
        }

        _isRestored = true;
        _restoreToMain(_navigationInfo, _contentView);
    }
}
