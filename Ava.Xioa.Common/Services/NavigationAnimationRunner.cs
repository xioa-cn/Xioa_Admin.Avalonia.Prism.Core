using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Ava.Xioa.Common.Services;

public static class NavigationAnimationRunner
{
    public static Task SlideFadeAsync(object? view, double fromOpacity, double toOpacity, double fromX, double toX,
        int milliseconds)
    {
        return AnimateAsync(view, fromOpacity, toOpacity, fromX, toX, fromY: 0, toY: 0, fromScale: 1, toScale: 1,
            milliseconds);
    }

    public static Task VerticalSlideFadeAsync(object? view, double fromOpacity, double toOpacity, double fromY,
        double toY, int milliseconds)
    {
        return AnimateAsync(view, fromOpacity, toOpacity, fromX: 0, toX: 0, fromY, toY, fromScale: 1, toScale: 1,
            milliseconds);
    }

    public static Task ScaleFadeAsync(object? view, double fromOpacity, double toOpacity, double fromScale,
        double toScale, int milliseconds)
    {
        return AnimateAsync(view, fromOpacity, toOpacity, fromX: 0, toX: 0, fromY: 0, toY: 0, fromScale, toScale,
            milliseconds);
    }

    public static void PrepareSlide(object? view, double opacity, double x)
    {
        PrepareSlide(view, opacity, x, y: 0);
    }

    public static void PrepareSlide(object? view, double opacity, double x, double y)
    {
        if (view is not Control control)
        {
            return;
        }

        SetVisualOnUiThread(control, opacity, x, y, scale: 1);
    }

    public static void PrepareScale(object? view, double opacity, double scale)
    {
        if (view is not Control control)
        {
            return;
        }

        SetVisualOnUiThread(control, opacity, x: 0, scale);
    }

    public static Task ResetAsync(object? view)
    {
        if (view is not Control control)
        {
            return Task.CompletedTask;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Reset(control);
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(() => { Reset(control); }).GetTask();
    }

    private static async Task AnimateAsync(
        object? view,
        double fromOpacity,
        double toOpacity,
        double fromX,
        double toX,
        double fromY,
        double toY,
        double fromScale,
        double toScale,
        int milliseconds)
    {
        if (view is not Control control)
        {
            return;
        }

        const int frameMilliseconds = 16;
        var frames = Math.Max(1, milliseconds / frameMilliseconds);

        for (var frame = 0; frame <= frames; frame++)
        {
            var progress = EaseOutCubic((double)frame / frames);
            var opacity = Lerp(fromOpacity, toOpacity, progress);
            var x = Lerp(fromX, toX, progress);
            var y = Lerp(fromY, toY, progress);
            var scale = Lerp(fromScale, toScale, progress);

            await SetVisualOnUiThreadAsync(control, opacity, x, y, scale);
            await Task.Delay(frameMilliseconds);
        }

        await SetVisualOnUiThreadAsync(control, toOpacity, toX, toY, toScale);
        if (toOpacity >= 1 && Math.Abs(toX) < 0.01 && Math.Abs(toY) < 0.01 && Math.Abs(toScale - 1) < 0.01)
        {
            await ResetAsync(control);
        }
    }

    private static void SetVisual(Control control, double opacity, double x, double y, double scale)
    {
        control.Opacity = opacity;
        control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        control.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(scale, scale),
                new TranslateTransform(x, y)
            }
        };
    }

    private static void SetVisualOnUiThread(Control control, double opacity, double x, double scale)
    {
        SetVisualOnUiThread(control, opacity, x, y: 0, scale);
    }

    private static void SetVisualOnUiThread(Control control, double opacity, double x, double y, double scale)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            SetVisual(control, opacity, x, y, scale);
            return;
        }

        Dispatcher.UIThread.Post(() => SetVisual(control, opacity, x, y, scale));
    }

    private static Task SetVisualOnUiThreadAsync(Control control, double opacity, double x, double y, double scale)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            SetVisual(control, opacity, x, y, scale);
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(() => SetVisual(control, opacity, x, y, scale)).GetTask();
    }

    private static void Reset(Control control)
    {
        control.Opacity = 1;
        control.RenderTransform = null;
    }

    private static double Lerp(double from, double to, double progress)
    {
        return from + ((to - from) * progress);
    }

    private static double EaseOutCubic(double progress)
    {
        var inverse = 1 - progress;
        return 1 - (inverse * inverse * inverse);
    }
}