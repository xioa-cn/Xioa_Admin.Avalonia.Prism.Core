﻿using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.WindowServices;

[PrismViewModel(typeof(IMainWindowServices), ServiceLifetime.Singleton)]
public partial class MainWindowViewModel : ChainReactiveObject<MainWindowViewModel>, IMainWindowServices
{
    [ObservableBindProperty] private double _Width;
    [ObservableBindProperty] private double _Height;
    [ObservableBindProperty] private bool _CanFullScreen;
    [ObservableBindProperty] private bool _CanMaximize;
    [ObservableBindProperty] private bool _CanMinimize;
    [ObservableBindProperty] private bool _IsTitleBarVisible;
    [ObservableBindProperty] private bool _CanPin;
    [ObservableBindProperty] private bool _CanResize;
    [ObservableBindProperty] private SukiWindow.TitleBarVisibilityMode _TitleBarVisibilityOnFullScreen;
    [ObservableBindProperty] private bool _ShowTitlebarBackground;
    [ObservableBindProperty] private bool _ShowBottomBorder;
    [ObservableBindProperty] private PixelPoint _Position;
    [ObservableBindProperty] private bool _CanMove;
    [ObservableBindProperty] private bool _IsVisible;
    [ObservableBindProperty] private double _Opacity;

    public MainWindowViewModel()
    {
        this.SetProperty(e => e.Width, 404)
                .SetProperty(e => e.Height, 384)
                    .SetProperty(e => e.Opacity, 0);
    }

    public void CenterScreen()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var window = desktop.MainWindow;

        if (window == null)
            return;

        // 确保窗口尺寸已确定（如果是动态尺寸，先更新布局）
        window.Measure(Size.Infinity);
        window.Arrange(new Rect(window.DesiredSize));

        // 获取主屏幕工作区（排除任务栏等）
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen == null)
            return;

        var workArea = screen.WorkingArea;

        // 计算居中位置（工作区中心 - 窗口一半尺寸）
        var x = workArea.X + (workArea.Width - window.Width) / 2;
        var y = workArea.Y + (workArea.Height - window.Height) / 2;

        // 设置窗口位置
        window.Position = new PixelPoint((int)x, (int)y);
    }
}