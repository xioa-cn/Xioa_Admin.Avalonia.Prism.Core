namespace Prism.Dialogs;

public interface IDialogService
{
    void Show(string name);

    void Show(string hostKey, string name);

    void Show(string name, IDialogParameters parameters);

    void Show(string hostKey, string name, IDialogParameters parameters);

    void Show(string name, Action<IDialogResult> callback);

    void Show(string hostKey, string name, Action<IDialogResult> callback);

    void Show(string name, IDialogParameters? parameters, Action<IDialogResult>? callback);

    void Show(string hostKey, string name, IDialogParameters? parameters, Action<IDialogResult>? callback);

    void ShowDialog(string name);

    void ShowDialog(string hostKey, string name);

    void ShowDialog(string name, IDialogParameters parameters);

    void ShowDialog(string hostKey, string name, IDialogParameters parameters);

    void ShowDialog(string name, Action<IDialogResult> callback);

    void ShowDialog(string hostKey, string name, Action<IDialogResult> callback);

    void ShowDialog(string name, IDialogParameters? parameters, Action<IDialogResult>? callback);

    void ShowDialog(string hostKey, string name, IDialogParameters? parameters, Action<IDialogResult>? callback);
}