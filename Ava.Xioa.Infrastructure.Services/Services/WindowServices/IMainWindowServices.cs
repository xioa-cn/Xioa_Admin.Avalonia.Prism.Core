using Avalonia;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Services.Services.WindowServices;

public interface IMainWindowServices
{
    double Width { get; set; }
    double Height { get; set; }
    double Opacity { get; set; }

    SukiWindow.TitleBarVisibilityMode TitleBarVisibilityOnFullScreen { get; set; }
    PixelPoint Position { get; set; }

    bool CanFullScreen { get; set; }
    bool CanMaximize { get; set; }
    bool CanMinimize { get; set; }
    bool IsTitleBarVisible { get; set; }
    bool CanPin { get; set; }
    bool CanResize { get; set; }
    bool ShowTitlebarBackground { get; set; }
    bool ShowBottomBorder { get; set; }
    bool CanMove { get; set; }
    bool IsVisible { get; set; }

    void CenterScreen();
}