using Avalonia.Controls;

namespace Prism.Dialogs;

public class DialogWindow : Window, IDialogWindow
{
    public DialogWindow()
    {
        Width = 420;
        Height = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}