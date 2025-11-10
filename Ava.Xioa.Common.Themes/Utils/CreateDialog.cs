using System.Threading.Tasks;
using Ava.Xioa.Common.Themes.Services.Services;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Utils;

public static class CreateDialogHelper
{
    public static SukiDialogBuilder CreateVmDialog(this ISukiDialogManager dialogManager,
        IDialogBaseable dialogBaseable, bool onDismisseResultToDefault = true)
    {
        var dialog = dialogManager.CreateDialog();
        dialogBaseable.SetDialog(dialog.Dialog);

        if (onDismisseResultToDefault)
        {
            dialog.Dialog.OnDismissed += sukiDialog => { sukiDialog.ResetToDefault(); };
        }

        return dialog;
    }

    public static SukiDialogBuilder WithAsync(this SukiDialogBuilder builder,
        TaskCompletionSource<bool>? completionSource = null)
    {
        builder.Completion = completionSource ?? new TaskCompletionSource<bool>();

        return builder;
    }
}