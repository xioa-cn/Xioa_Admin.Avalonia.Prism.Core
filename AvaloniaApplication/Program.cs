using Avalonia;
using System;
using Ava.Xioa.Common.Utils;

namespace AvaloniaApplication;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppAuthor.DllCreateTime = System.IO.File.GetLastWriteTime(typeof(Program).Assembly.Location);
        if (!App.Detection)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}