namespace Prism.Dialogs;

public interface IDialogAware
{
    string? Title { get; }

    event Action<IDialogResult>? RequestClose;

    DialogCloseListener DialogCloseListener { get; set; }

    bool CanCloseDialog();

    void OnDialogClosed();

    void OnDialogOpened(IDialogParameters parameters);
}