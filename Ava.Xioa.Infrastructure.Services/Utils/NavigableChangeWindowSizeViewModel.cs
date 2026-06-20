using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ava.Xioa.Common;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure.Services.Utils;

public abstract class NavigableChangeWindowSizeViewModel : NavigableViewModelObject
{
    private const double WindowResizeAnimationMilliseconds = 220d;
    private const int WindowResizeFrameDelayMilliseconds = 16;
    private static readonly object WindowResizeAnimationSyncRoot = new();
    private static CancellationTokenSource? _windowResizeAnimationCts;
    private readonly IMainWindowServices _mainWindowServices;

    public NavigableChangeWindowSizeViewModel(
        IEventAggregator eventAggregator,
        IRegionManager regionManager,
        IMainWindowServices mainWindowServices)
        : base(eventAggregator, regionManager)
    {
        _mainWindowServices = mainWindowServices;
    }

    protected abstract Size AfterChangeSize { get; }

    protected virtual Animation? WindowChangeAnimation { get; private set; }

    public virtual void ChangeMainWindowSize()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var window = desktop.MainWindow;
        if (window is null)
        {
            return;
        }

        var cancellationToken = ResetWindowResizeAnimation();
        _ = AnimateMainWindowSizeAsync(window, AfterChangeSize, cancellationToken);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        ChangeMainWindowSize();
    }

    private static CancellationToken ResetWindowResizeAnimation()
    {
        lock (WindowResizeAnimationSyncRoot)
        {
            _windowResizeAnimationCts?.Cancel();
            _windowResizeAnimationCts?.Dispose();
            _windowResizeAnimationCts = new CancellationTokenSource();
            return _windowResizeAnimationCts.Token;
        }
    }

    private async Task AnimateMainWindowSizeAsync(Window window, Size targetSize, CancellationToken cancellationToken)
    {
        try
        {
            var startWidth = GetCurrentWindowWidth(window);
            var startHeight = GetCurrentWindowHeight(window);
            var startPosition = window.Position;
            var targetPosition = GetCenteredPosition(window, targetSize) ?? startPosition;

            if (IsClose(startWidth, targetSize.Width) &&
                IsClose(startHeight, targetSize.Height) &&
                startPosition == targetPosition)
            {
                _ = WindowChangeAnimation?.RunAsync(window);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < WindowResizeAnimationMilliseconds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var progress = stopwatch.Elapsed.TotalMilliseconds / WindowResizeAnimationMilliseconds;
                ApplyWindowSizeFrame(
                    window,
                    startWidth,
                    startHeight,
                    startPosition,
                    targetSize,
                    targetPosition,
                    EaseOutCubic(progress));

                await Task.Delay(WindowResizeFrameDelayMilliseconds, cancellationToken);
            }

            ApplyFinalWindowSize(window, targetSize, targetPosition);
            _ = WindowChangeAnimation?.RunAsync(window);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ApplyWindowSizeFrame(
        Window window,
        double startWidth,
        double startHeight,
        PixelPoint startPosition,
        Size targetSize,
        PixelPoint targetPosition,
        double progress)
    {
        _mainWindowServices.Width = Lerp(startWidth, targetSize.Width, progress);
        _mainWindowServices.Height = Lerp(startHeight, targetSize.Height, progress);
        window.Position = Lerp(startPosition, targetPosition, progress);
    }

    private void ApplyFinalWindowSize(Window window, Size targetSize, PixelPoint targetPosition)
    {
        _mainWindowServices.Width = targetSize.Width;
        _mainWindowServices.Height = targetSize.Height;
        window.Position = targetPosition;
    }

    private static PixelPoint? GetCenteredPosition(Window window, Size targetSize)
    {
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen is null)
        {
            return null;
        }

        var scaling = screen.Scaling;
        var scaledWidth = targetSize.Width * scaling;
        var scaledHeight = targetSize.Height * scaling;
        var newLeft = screen.WorkingArea.X + (int)((screen.WorkingArea.Width - scaledWidth) / 2);
        var newTop = screen.WorkingArea.Y + (int)((screen.WorkingArea.Height - scaledHeight) / 2);
        return new PixelPoint(newLeft, newTop);
    }

    private static double GetCurrentWindowWidth(Window window)
    {
        return window.Bounds.Width > 0 ? window.Bounds.Width : window.Width;
    }

    private static double GetCurrentWindowHeight(Window window)
    {
        return window.Bounds.Height > 0 ? window.Bounds.Height : window.Height;
    }

    private static double EaseOutCubic(double value)
    {
        var clamped = Math.Clamp(value, 0d, 1d);
        return 1d - Math.Pow(1d - clamped, 3d);
    }

    private static double Lerp(double start, double end, double progress)
    {
        return start + ((end - start) * progress);
    }

    private static PixelPoint Lerp(PixelPoint start, PixelPoint end, double progress)
    {
        return new PixelPoint(
            (int)Math.Round(Lerp(start.X, end.X, progress)),
            (int)Math.Round(Lerp(start.Y, end.Y, progress)));
    }

    private static bool IsClose(double first, double second)
    {
        return Math.Abs(first - second) < 0.5d;
    }
}
