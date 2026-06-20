using System;

namespace Prism.Dialogs;

public class DialogCloseListener
{
    private readonly Action<IDialogResult> _callback;

    public DialogCloseListener(Action<IDialogResult> callback)
    {
        _callback = callback;
    }

    public void Invoke()
    {
        _callback(new DialogResult(ButtonResult.None));
    }

    public void Invoke(ButtonResult result)
    {
        _callback(new DialogResult(result));
    }

    public void Invoke(IDialogParameters parameters)
    {
        _callback(new DialogResult(ButtonResult.None, parameters));
    }

    public void Invoke(ButtonResult result, IDialogParameters parameters)
    {
        _callback(new DialogResult(result, parameters));
    }
}