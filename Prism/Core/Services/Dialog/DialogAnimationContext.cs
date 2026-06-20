using Avalonia.Controls;

namespace Prism.Dialogs;

public sealed class DialogAnimationContext
{
    public DialogAnimationContext(object dialog, Control? host, bool isWindow, IDialogParameters parameters)
    {
        Dialog = dialog;
        Host = host;
        IsWindow = isWindow;
        Parameters = parameters;
    }

    public object Dialog { get; }

    public Control? Host { get; }

    public bool IsWindow { get; }

    public IDialogParameters Parameters { get; }
}