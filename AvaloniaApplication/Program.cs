using Avalonia;
using System;
using Ava.Xioa.Common.Utils;
using Avalonia.Dialogs;

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
            BuildAvaloniaApp(args)
                .StartWithClassicDesktopLifetime(args);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(string[] args)
    {
        var appBuilder = AppBuilder.Configure(() => new App(args))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
        
        return appBuilder;
    }
}