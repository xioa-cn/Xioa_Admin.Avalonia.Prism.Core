using Ava.Xioa.Common.Attributes;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Common.Services;

[PrismService(typeof(ClipboardService), ServiceLifetime.Singleton)]
public class ClipboardService(IApplicationLifetime liftime)
{
    public void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        IClipboard? clipboard = null;

        if (liftime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            clipboard = TopLevel.GetTopLevel(desktop.MainWindow)?.Clipboard;
            ;
        }

        else if (liftime is ISingleViewApplicationLifetime singleView)
        {
            clipboard = TopLevel.GetTopLevel(singleView.MainView)?.Clipboard;
            // singleView.MainView?.Clipboard?.SetTextAsync(text);
        }


        clipboard?.SetTextAsync(text);
    }
}