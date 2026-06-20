namespace Prism.Dialogs;

public class DialogResult : IDialogResult
{
    public DialogResult(ButtonResult result)
        : this(result, new DialogParameters())
    {
    }

    public DialogResult(ButtonResult result, IDialogParameters parameters)
    {
        Result = result;
        Parameters = parameters;
    }

    public ButtonResult Result { get; }

    public IDialogParameters Parameters { get; }
}