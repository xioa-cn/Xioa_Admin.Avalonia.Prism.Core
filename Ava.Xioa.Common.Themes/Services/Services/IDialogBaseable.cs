using System.Windows.Input;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface IDialogBaseable
{
    ISukiDialog? SukiDialog { get; set; }

    public void SetDialog(ISukiDialog sukiDialog)
    {
        SukiDialog = sukiDialog;
    }

    void CloseDialog()
    {
        SukiDialog?.Dismiss();
        SukiDialog?.ResetToDefault();
    }
}

public interface IDialogBtnCommand
{
    ICommand CancelCommand { get; }
    ICommand OkCommand { get; }
}