using Avalonia.Controls;
using Avalonia.Media;

namespace Prism.Dialogs;

public sealed class DialogOptions
{
    public static DialogAnimationKind GlobalDefaultAnimationKind { get; set; } = DialogAnimationKind.None;

    public static TimeSpan GlobalDefaultAnimationDuration { get; set; } = TimeSpan.FromMilliseconds(160);

    public double? Width { get; set; }

    public double? Height { get; set; }

    public bool? CanResize { get; set; }

    public bool? Topmost { get; set; }

    public bool? ShowInTaskbar { get; set; }

    public WindowStartupLocation? WindowStartupLocation { get; set; }

    public bool CloseOnEscape { get; set; } = true;

    public bool CloseOnOverlayClick { get; set; } = true;

    public IBrush? OverlayBrush { get; set; }

    public TimeSpan? Timeout { get; set; }

    public ButtonResult TimeoutResult { get; set; } = ButtonResult.None;

    public IDialogAnimation? Animation { get; set; }

    public DialogAnimationKind? AnimationKind { get; set; }

    public TimeSpan? AnimationDuration { get; set; }

    public Action<DialogAnimationContext>? Opening { get; set; }

    public Action<DialogAnimationContext>? Closing { get; set; }

    public Func<DialogAnimationContext, Task>? OpeningAsync { get; set; }

    public Func<DialogAnimationContext, Task>? ClosingAsync { get; set; }

    public static DialogOptions From(IDialogParameters? parameters)
    {
        var options = new DialogOptions();
        if (parameters is null)
        {
            return options;
        }

        options.Width = TryGetParameter<double>(parameters, nameof(Width));
        options.Height = TryGetParameter<double>(parameters, nameof(Height));
        options.CanResize = TryGetParameter<bool>(parameters, nameof(CanResize));
        options.Topmost = TryGetParameter<bool>(parameters, nameof(Topmost));
        options.ShowInTaskbar = TryGetParameter<bool>(parameters, nameof(ShowInTaskbar));
        options.WindowStartupLocation = TryGetParameter<WindowStartupLocation>(parameters, nameof(WindowStartupLocation));
        options.Timeout = TryGetParameter<TimeSpan>(parameters, nameof(Timeout));
        options.TimeoutResult = parameters.ContainsKey(nameof(TimeoutResult))
            ? parameters.GetValue<ButtonResult>(nameof(TimeoutResult))
            : options.TimeoutResult;
        options.Animation = TryGetParameter<IDialogAnimation>(parameters, nameof(Animation));
        options.AnimationKind = TryGetParameter<DialogAnimationKind>(parameters, nameof(AnimationKind));
        options.AnimationDuration = TryGetParameter<TimeSpan>(parameters, nameof(AnimationDuration));
        options.Opening = TryGetParameter<Action<DialogAnimationContext>>(parameters, nameof(Opening));
        options.Closing = TryGetParameter<Action<DialogAnimationContext>>(parameters, nameof(Closing));
        options.OpeningAsync = TryGetParameter<Func<DialogAnimationContext, Task>>(parameters, nameof(OpeningAsync));
        options.ClosingAsync = TryGetParameter<Func<DialogAnimationContext, Task>>(parameters, nameof(ClosingAsync));
        if (parameters.ContainsKey(nameof(CloseOnEscape)))
        {
            options.CloseOnEscape = parameters.GetValue<bool>(nameof(CloseOnEscape));
        }

        if (parameters.ContainsKey(nameof(CloseOnOverlayClick)))
        {
            options.CloseOnOverlayClick = parameters.GetValue<bool>(nameof(CloseOnOverlayClick));
        }

        var overlayBrush = TryGetParameter<string>(parameters, nameof(OverlayBrush));
        if (!string.IsNullOrWhiteSpace(overlayBrush) && Color.TryParse(overlayBrush, out var color))
        {
            options.OverlayBrush = new SolidColorBrush(color);
        }

        return options;
    }

    public IDialogAnimation? GetAnimation()
    {
        if (Animation is not null)
        {
            return Animation;
        }

        var kind = AnimationKind ?? GlobalDefaultAnimationKind;
        if (kind == DialogAnimationKind.None)
        {
            return null;
        }

        return new DefaultDialogAnimation(kind)
        {
            Duration = AnimationDuration ?? GlobalDefaultAnimationDuration
        };
    }

    private static T? TryGetParameter<T>(IDialogParameters parameters, string key)
    {
        if (!parameters.ContainsKey(key))
        {
            return default;
        }

        return parameters.GetValue<T>(key);
    }
}