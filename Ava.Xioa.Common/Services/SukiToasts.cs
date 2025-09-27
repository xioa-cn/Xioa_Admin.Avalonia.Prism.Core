using System;
using Ava.Xioa.Common.Attributes;
using Avalonia.Controls.Notifications;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Toasts;

namespace Ava.Xioa.Common.Services;

[PrismService(typeof(ToastsService), Lifetime = ServiceLifetime.Singleton)]
public class ToastsService(ISukiToastManager sukiToastManager)
{
    public void ShowToast(NotificationType toastType, string title, string message, TimeSpan? timeSpan = null)
    {
        if (timeSpan is null)
        {
            timeSpan = TimeSpan.FromSeconds(3);
        }

        sukiToastManager.CreateToast()
            .WithTitle($"{title}!")
            .WithContent(
                $"{message}")
            .OfType(toastType)
            .Dismiss().After((TimeSpan)timeSpan)
            .Dismiss().ByClicking()
            .Queue();
    }
}