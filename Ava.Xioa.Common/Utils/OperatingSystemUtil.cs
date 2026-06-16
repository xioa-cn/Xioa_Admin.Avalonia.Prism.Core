using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Ava.Xioa.Common.Utils;

public static class OperatingSystemUtil
{
    public static bool IsDesktopPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
               || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
               || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
               || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)
               || RuntimeInformation.OSDescription.Contains("BSD");
    }


    public static TopLevel? GetCurrentTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 优先返回主窗口
            if (desktop.MainWindow != null)
                return desktop.MainWindow;

            // 主窗口不存在时，找第一个激活的窗口
            return desktop.Windows.FirstOrDefault(w => w.IsActive)
                   ?? desktop.Windows.FirstOrDefault();
        }

        // WASM/移动端单视图场景
        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            return singleView.MainView as TopLevel;
        }

        return null;
    }
}