using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Prism.Dialogs;

public sealed class DefaultDialogAnimation : IDialogAnimation
{
    public DefaultDialogAnimation()
    {
    }

    public DefaultDialogAnimation(DialogAnimationKind kind)
    {
        Kind = kind;
    }

    public DialogAnimationKind Kind { get; set; } = DialogAnimationKind.Scale;

    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(160);

    public double SlideDistance { get; set; } = 24;

    public double OpeningScale { get; set; } = 0.96;

    public Task OnOpeningAsync(DialogAnimationContext context)
    {
        return AnimateAsync(context.Dialog, opening: true);
    }

    public Task OnClosingAsync(DialogAnimationContext context)
    {
        return AnimateAsync(context.Dialog, opening: false);
    }

    private async Task AnimateAsync(object dialog, bool opening)
    {
        if (Kind == DialogAnimationKind.None || dialog is not Control control)
        {
            return;
        }

        var originalOpacity = control.Opacity;
        var originalTransform = control.RenderTransform;
        var originalOrigin = control.RenderTransformOrigin;
        var frames = Math.Max(1, (int)Math.Ceiling(Duration.TotalMilliseconds / 16d));

        for (var frame = 0; frame <= frames; frame++)
        {
            var rawProgress = (double)frame / frames;
            var progress = opening ? EaseOutCubic(rawProgress) : EaseInCubic(rawProgress);
            var opacity = opening ? Lerp(0, originalOpacity, progress) : Lerp(originalOpacity, 0, progress);
            var scale = GetScale(opening, progress);
            var (x, y) = GetTranslation(opening, progress);

            await SetVisualAsync(control, opacity, x, y, scale).ConfigureAwait(true);
            await Task.Delay(16).ConfigureAwait(true);
        }

        if (opening)
        {
            await RestoreVisualAsync(control, originalOpacity, originalTransform, originalOrigin).ConfigureAwait(true);
        }
    }

    private double GetScale(bool opening, double progress)
    {
        if (Kind != DialogAnimationKind.Scale)
        {
            return 1;
        }

        return opening
            ? Lerp(OpeningScale, 1, progress)
            : Lerp(1, OpeningScale, progress);
    }

    private (double X, double Y) GetTranslation(bool opening, double progress)
    {
        var start = opening ? 1 - progress : progress;
        return Kind switch
        {
            DialogAnimationKind.SlideUp => (0, SlideDistance * start),
            DialogAnimationKind.SlideDown => (0, -SlideDistance * start),
            DialogAnimationKind.SlideLeft => (SlideDistance * start, 0),
            DialogAnimationKind.SlideRight => (-SlideDistance * start, 0),
            _ => (0, 0)
        };
    }

    private static Task SetVisualAsync(Control control, double opacity, double x, double y, double scale)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            SetVisual(control, opacity, x, y, scale);
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(() => SetVisual(control, opacity, x, y, scale)).GetTask();
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

    private static Task RestoreVisualAsync(Control control, double opacity, ITransform? transform, RelativePoint transformOrigin)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RestoreVisual(control, opacity, transform, transformOrigin);
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(() => RestoreVisual(control, opacity, transform, transformOrigin)).GetTask();
    }

    private static void RestoreVisual(Control control, double opacity, ITransform? transform, RelativePoint transformOrigin)
    {
        control.Opacity = opacity;
        control.RenderTransform = transform;
        control.RenderTransformOrigin = transformOrigin;
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

    private static double EaseInCubic(double progress)
    {
        return progress * progress * progress;
    }
}