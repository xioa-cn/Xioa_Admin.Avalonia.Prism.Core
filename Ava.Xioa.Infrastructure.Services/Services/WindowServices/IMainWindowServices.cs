using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using SukiUI.Controls;
using NavigableBarInfoModel = Ava.Xioa.Common.Models.NavigableBarInfoModel;

namespace Ava.Xioa.Infrastructure.Services.Services.WindowServices;

public interface IMainWindowServices
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Opacity { get; set; }

    public SukiWindow.TitleBarVisibilityMode TitleBarVisibilityOnFullScreen { get; set; }
    public PixelPoint Position { get; set; }
    public WindowState WindowState { get; set; }

    public bool CanFullScreen { get; set; }
    public bool CanMaximize { get; set; }
    public bool CanMinimize { get; set; }
    public bool IsTitleBarVisible { get; set; }
    public bool CanPin { get; set; }
    public bool CanResize { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool ShowTitlebarBackground { get; set; }
    public bool ShowBottomBorder { get; set; }
    public bool CanMove { get; set; }
    public bool IsVisible { get; set; }
    public bool IsMenuVisible { get; set; }

    public WindowIcon Icon { get; set; }

    public void CenterScreen();

    void ToNotifyTrayIcon();

    public string ApplicationInformation { get; }

    bool ChangeAppFontFamily(string fontFamily);

    ObservableCollection<NavigableBarInfoModel> NavigableBarInfos { get; set; }

    bool HomeRegionLoading { get; set; }

    void HomeRegionLoadingInvoke(Action action);

    Task HomeRegionLoadingInvokeAsync(Func<Task> action);
}