using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.WindowServices;

[PrismViewModel(typeof(IMainWindowServices), ServiceLifetime.Singleton)]
public partial class MainWindowViewModel : ReactiveObject, IMainWindowServices
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
    [ObservableBindProperty] private bool _ShowInTaskbar;
    [ObservableBindProperty] private WindowState _WindowState;
    [ObservableBindProperty] private WindowIcon _Icon;
    [ObservableBindProperty] private bool _IsMenuVisible;

    private readonly IEventAggregator _eventAggregator;

    public MainWindowViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        this.Width = 404;
        this.Height = 384;
        this.Opacity = 0;
        this.IsVisible = true;
        this.Opacity = 1;
        this.WindowState = WindowState.Normal;

        _eventAggregator.GetEvent<WindowIconEvent>().Subscribe(IconChanged, ThreadOption.UIThread, true,
            filter => filter.TokenKey == "WindowIcon");
    }

    private void IconChanged(TokenKeyPubSubEvent<WindowIcon> obj)
    {
        this.Icon = obj.Value;
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
        
        var scaling = screen.Scaling;
        var scaledWidth = window.Width * scaling;
        var scaledHeight = window.Height * scaling;
        var newLeft = (int)((screen.WorkingArea.Width - scaledWidth) / 2);
        var newTop = (int)((screen.WorkingArea.Height - scaledHeight) / 2);
        window.Position = new PixelPoint(newLeft, newTop);
        
        // var workArea = screen.WorkingArea;
        //
        // // 计算居中位置（工作区中心 - 窗口一半尺寸）
        // var x = workArea.X + (workArea.Width - window.Width) / 2;
        // var y = workArea.Y + (workArea.Height - window.Height) / 2;
        //
        // // 设置窗口位置
        // window.Position = new PixelPoint((int)x, (int)y);
    }

    public void ToNotifyTrayIcon()
    {
        this.IsVisible = false;
        this.Opacity = 0;
    }

    public bool ChangeAppFontFamily(string fontFamily)
    {
        if (!(Application.Current?.Resources.TryGetResource(fontFamily, null, out var fontObj) ?? false)) return false;
        Dispatcher.UIThread.Invoke(() =>
        {
            if (fontObj is FontFamily fontFamilyObj)
            {
                Application.Current.Resources["DefaultFontFamily"] = fontFamilyObj;
            }
        });
        return fontObj is FontFamily;
    }

    public string ApplicationInformation =>
        $"{AppAuthor.DllCreateTime:yyyy} © AvaloniaApplication BY {AppAuthor.Author}. {AppAuthor.DllCreateTime.TimeYearMonthDayHourString()}";
}