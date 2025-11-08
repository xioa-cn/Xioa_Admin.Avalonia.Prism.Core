using Avalonia.Controls.Notifications;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Utils;

public static class DialogHelper
{
    public static SukiDialogBuilder SetMessage(this ISukiDialogManager dialogManager, string message, string title)
    {
        return dialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(message)
            .Dismiss().ByClickingBackground();
    }

    public static SukiDialogBuilder ErrorMessage(this ISukiDialogManager dialogManager, string message, string title)
    {
        return dialogManager.SetMessage(message, title).OfType(NotificationType.Error);
    }

    public static SukiDialogBuilder WarningMessage(this ISukiDialogManager dialogManager, string message, string title)
    {
        return dialogManager.SetMessage(message, title).OfType(NotificationType.Warning);
    }

    public static SukiDialogBuilder InfoMessage(this ISukiDialogManager dialogManager, string message, string title)
    {
        return dialogManager.SetMessage(message, title).OfType(NotificationType.Information);
    }

    public static SukiDialogBuilder SuccessMessage(this ISukiDialogManager dialogManager, string message, string title)
    {
        return dialogManager.SetMessage(message, title).OfType(NotificationType.Success);
    }

    // public static SukiDialogBuilder SetActionButton(this ISukiDialogManager dialogManager)
    // {
    //     
    // }
}