using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.HomeServices;

[PrismViewModel(typeof(IHomeServices), ServiceLifetime.Singleton)]
public class HomeViewModel : NavigableChangeWindowSizeViewModel, IHomeServices
{
    private readonly IMainWindowServices _mainWindowServices;

    public HomeViewModel(IRegionManager regionManager, IMainWindowServices mainWindowServices) : base(regionManager,
        mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
    }

    protected override Size AfterChangeSize { get; } = new Size(1536, 808);

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mainWindowServices.CanFullScreen = true;
        _mainWindowServices.CanMinimize = true;
        _mainWindowServices.CanMaximize = true;
        _mainWindowServices.IsTitleBarVisible = true;
        _mainWindowServices.CanPin = true;
        _mainWindowServices.CanResize = true;
        _mainWindowServices.ShowTitlebarBackground = true;
        _mainWindowServices.ShowBottomBorder = true;
        _mainWindowServices.CanMove = true;
        _mainWindowServices.TitleBarVisibilityOnFullScreen = SukiWindow.TitleBarVisibilityMode.Visible;
        base.OnNavigatedTo(navigationContext);
    }

    // public override void OnNavigatedTo(NavigationContext navigationContext)
    // {
    //     if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
    //     var window = desktop.MainWindow;
    //     if (window == null) return;
    //     // 从当前大小动画过渡到新大小
    //     var targetSize = new Size(1536, 808);
    //     var duration = TimeSpan.FromSeconds(1.25);
    //   
    //     var screen = window.Screens.ScreenFromVisual(window);
    //     if (screen != null)
    //     {
    //         var scaling = screen.Scaling;
    //         var scaledWidth = targetSize.Width * scaling;
    //         var scaledHeight = targetSize.Height * scaling;
    //
    //         var newLeft = (int)((screen.WorkingArea.Width - scaledWidth) / 2);
    //         var newTop = (int)((screen.WorkingArea.Height - scaledHeight) / 2);
    //         window.Position = new PixelPoint(newLeft, newTop);
    //     }
    //
    //     window.Width = targetSize.Width;
    //     window.Height = targetSize.Height;
    //     
    //     // var sizeAnimation = new Animation
    //     // {
    //     //     Duration = duration,
    //     //     Easing = new CubicEaseInOut(),
    //     //     Children =
    //     //     {
    //     //         new KeyFrame
    //     //         {
    //     //             Cue = new Cue(0),
    //     //             Setters = 
    //     //             { 
    //     //                 new Setter(Layoutable.WidthProperty, window.Width),
    //     //                 new Setter(Layoutable.HeightProperty, window.Height)
    //     //             }
    //     //         },
    //     //         new KeyFrame
    //     //         {
    //     //             Cue = new Cue(1),
    //     //             Setters = 
    //     //             { 
    //     //                 new Setter(Layoutable.WidthProperty, targetSize.Width),
    //     //                 new Setter(Layoutable.HeightProperty, targetSize.Height)
    //     //             }
    //     //         }
    //     //     }
    //     // };
    //
    //
    //     //sizeAnimation.RunAsync(window);
    //
    //     // 立即计算并设置居中位置(考虑DPI缩放)
    // }
}