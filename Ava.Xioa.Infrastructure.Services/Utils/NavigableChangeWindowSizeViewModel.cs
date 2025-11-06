using Ava.Xioa.Common;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Events;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure.Services.Utils;

public abstract class NavigableChangeWindowSizeViewModel : NavigableViewModelObject
{
    private readonly IMainWindowServices _mainWindowServices;

    public NavigableChangeWindowSizeViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
        IMainWindowServices mainWindowServices) :
        base(eventAggregator, regionManager)
    {
        _mainWindowServices = mainWindowServices;
    }

    protected abstract Size AfterChangeSize { get; }

    protected virtual Animation? WindowChangeAnimation { get; private set; }

    public virtual void ChangeMainWindowSize()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var window = desktop.MainWindow;
        if (window == null) return;
        var targetSize = AfterChangeSize;
        //var duration = TimeSpan.FromSeconds(1.25);

        // var screen = window.Screens.ScreenFromVisual(window);
        // if (screen != null)
        // {
        //     var scaling = screen.Scaling;
        //     var scaledWidth = targetSize.Width * scaling;
        //     var scaledHeight = targetSize.Height * scaling;
        //
        //     var newLeft = (int)((screen.WorkingArea.Width - scaledWidth) / 2);
        //     var newTop = (int)((screen.WorkingArea.Height - scaledHeight) / 2);
        //     // window.Position = new PixelPoint(newLeft, newTop);
        // }

        window.Width = targetSize.Width;
        window.Height = targetSize.Height;
        _mainWindowServices.CenterScreen();
        WindowChangeAnimation?.RunAsync(window);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        ChangeMainWindowSize();
    }
}