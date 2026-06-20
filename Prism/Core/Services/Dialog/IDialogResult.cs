namespace Prism.Dialogs;

public interface IDialogResult
{
    ButtonResult Result { get; }

    IDialogParameters Parameters { get; }
}