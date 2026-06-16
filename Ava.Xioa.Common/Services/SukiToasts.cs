using System;
using Ava.Xioa.Common.Attributes;
using Avalonia.Controls.Notifications;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Toasts;

namespace Ava.Xioa.Common.Services;

public interface IToastsService
{
    void ShowToast(NotificationType toastType, string title, string message, TimeSpan? timeSpan = null);
    void ShowToast(NotificationType toastType, string title, string message, double milliseconds);
    void ShowInformation(string title, string message, double milliseconds = 3000);
    void ShowSuccess(string title, string message, double milliseconds = 3000);
    void ShowWarning(string title, string message, double milliseconds = 3000);
    void ShowError(string title, string message, double milliseconds = 3000);
}

[PrismService(typeof(IToastsService), Lifetime = ServiceLifetime.Singleton)]
public class ToastsService(ISukiToastManager sukiToastManager) : IToastsService
{
    public void ShowToast(NotificationType toastType, string title, string message, TimeSpan? timeSpan = null)
    {
        timeSpan ??= TimeSpan.FromSeconds(3);

        sukiToastManager.CreateToast()
            .WithTitle($"{title}!")
            .WithContent(
                $"{message}")
            .OfType(toastType)
            .Dismiss().After((TimeSpan)timeSpan)
            .Dismiss().ByClicking()
            .Queue();
    }

    public void ShowToast(NotificationType toastType, string title, string message, double milliseconds)
    {
        TimeSpan? timeSpan = TimeSpan.FromMilliseconds(milliseconds);

        ShowToast(toastType, title, message, timeSpan);
    }

    public void ShowInformation(string title, string message, double milliseconds = 3000)
    {
        ShowToast(NotificationType.Information, title, message, milliseconds);
    }

    public void ShowSuccess(string title, string message, double milliseconds = 3000)
    {
        ShowToast(NotificationType.Success, title, message, milliseconds);
    }

    public void ShowWarning(string title, string message, double milliseconds = 3000)
    {
        ShowToast(NotificationType.Warning, title, message, milliseconds);
    }

    public void ShowError(string title, string message, double milliseconds = 3000)
    {
        ShowToast(NotificationType.Error, title, message, milliseconds);
    }
}