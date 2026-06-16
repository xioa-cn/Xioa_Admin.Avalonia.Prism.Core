using Avalonia;
using System;
using Ava.Xioa.Common.Utils;
using Avalonia.Dialogs;

namespace AvaloniaApplication;

static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppAuthor.DllCreateTime = System.IO.File.GetLastWriteTime(typeof(Program).Assembly.Location);

        var app = new App(args);

        if (app.IsAnotherInstanceRunning)
        {
            return;
        }

        app.BuildAvaloniaApp(args)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(this App app, string[] args)
    {
        
        var appBuilder = AppBuilder.Configure(() => app)
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        return appBuilder;
    }
}
