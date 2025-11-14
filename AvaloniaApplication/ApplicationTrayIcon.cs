using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Styling;
using Prism.Events;
using Prism.Ioc;
using SkiaSharp;
using Svg.Skia;

namespace AvaloniaApplication;

public partial class App
{
    private IEventAggregator? _eventAggregator;

    private TrayIcon? _trayIcon;

    /// <summary>
    /// 加载SVG并应用指定颜色
    /// </summary>
    /// <param name="svgPath">SVG文件路径，支持avares://格式</param>
    /// <param name="width">输出位图宽度</param>
    /// <param name="height">输出位图高度</param>
    /// <param name="colorCode">要应用的颜色代码，例如"#FFFFFF"</param>
    /// <returns>处理后的位图</returns>
    public static Avalonia.Media.Imaging.Bitmap LoadSvgWithColor(string svgPath, int width, int height,
        string colorCode)
    {
        // 读取SVG文件内容
        string svgContent;

        // 处理avares://资源路径
        if (svgPath.StartsWith("avares://"))
        {
            using var stream = AssetLoader.Open(new Uri(svgPath));
            using var reader = new StreamReader(stream);
            svgContent = reader.ReadToEnd();
        }
        else
        {
            // 直接从文件系统读取
            svgContent = File.ReadAllText(svgPath);
        }

        // 替换常见的颜色属性
        svgContent = svgContent.Replace("fill=\"#0000FF\"", $"fill=\"{colorCode}\"");
        svgContent = svgContent.Replace("stroke=\"#0000FF\"", $"stroke=\"{colorCode}\"");

        // 将修改后的SVG内容保存到内存流
        using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));

        // 加载修改后的SVG
        var svg = new Svg.Skia.SKSvg();
        svg.Load(memStream);

        // 创建一个适当大小的位图
        var bitmap = new SKBitmap(width, height);
        var canvas = new SKCanvas(bitmap);

        // 计算缩放以适应指定尺寸
        float scaleX = width / svg.Picture?.CullRect.Width ?? 100;
        float scaleY = height / svg.Picture?.CullRect.Height ?? 100;
        float scale = Math.Min(scaleX, scaleY);

        // 应用缩放并居中
        canvas.Scale(scale);

        // 绘制SVG
        canvas.DrawPicture(svg.Picture);
        canvas.Flush();

        // 转换为Avalonia位图
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var outStream = new MemoryStream();
        data.SaveTo(outStream);
        outStream.Position = 0;

        return new Avalonia.Media.Imaging.Bitmap(outStream);
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    public void InitializeTrayIcon()
    {
        _eventAggregator = Container.Resolve<IEventAggregator>();
        var icons = TrayIcon.GetIcons(this);
        if (icons != null && icons.Count > 0)
        {
            _trayIcon = icons[0]; // 获取第一个托盘图标

            _trayIcon.Menu = new NativeMenu();
            _trayIcon.Menu.Items.Add(new NativeMenuItem { Header = "打开", Command = new RelayCommand(OpenMainWindow) });
            _trayIcon.Menu.Items.Add(new NativeMenuItem
                { Header = "退出", Command = new RelayCommand(() => ExitApplication()) });

            _trayIcon.Clicked += TrayIcon_Clicked;
            // 初始设置
            UpdateTrayIconForTheme(RequestedThemeVariant);
        }

        _eventAggregator.GetEvent<ThemeChangedEvent>().Subscribe(color => UpdateTrayIconColor(color.Value)
            , ThreadOption.UIThread, true, filter => filter.TokenKey == "SystemColor");
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        OpenMainWindow();
    }

    private void ExitApplication(int exitCode = 0)
    {
        this.Exit(exitCode);
        Environment.Exit(exitCode);
    }

    private void OpenMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        if (desktop.MainWindow is not null && desktop.MainWindow.IsVisible)
        {
             desktop.MainWindow.Hide();
             return;
        }

        desktop.MainWindow?.Show();
        desktop.MainWindow?.Activate();


        if (desktop.MainWindow is null) return;
        desktop.MainWindow.Topmost = true;
        _ = Task.Delay(100).ContinueWith(_ => { desktop.MainWindow.Topmost = false; },
            TaskScheduler.FromCurrentSynchronizationContext());

        desktop.MainWindow.WindowState = WindowState.Normal;
    }

    /// <summary>
    /// 主题变化事件处理
    /// </summary>
    private void OnThemeChanged(object sender, EventArgs e)
    {
        UpdateTrayIconForTheme(RequestedThemeVariant);
    }

    /// <summary>
    /// 根据主题更新托盘图标
    /// </summary>
    private void UpdateTrayIconForTheme(ThemeVariant? theme)
    {
        if (_trayIcon == null) return;

        // 根据主题选择颜色
        string colorCode = theme == ThemeVariant.Dark ? "#FF0000" : "#FF0000";
        UpdateTrayIconColor(colorCode);
    }

    /// <summary>
    /// 使用指定颜色更新托盘图标
    /// </summary>
    /// <param name="colorCode">颜色代码，例如"#FFFFFF"</param>
    public void UpdateTrayIconColor(string colorCode)
    {
        if (_trayIcon == null) return;

        string svgPath = "avares://AvaloniaApplication/Assets/battledotnet.svg";

        // 渲染SVG为位图，应用指定颜色
        var bitmap = LoadSvgWithColor(svgPath, 16, 16, colorCode);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            var icon = new WindowIcon(bitmap);
            // desktop.MainWindow.Icon = icon;
            _eventAggregator?.GetEvent<WindowIconEvent>()
                .Publish(new TokenKeyPubSubEvent<WindowIcon>("WindowIcon", icon));
        }

        // 更新托盘图标
        _trayIcon.Icon = new WindowIcon(bitmap);
    }
}