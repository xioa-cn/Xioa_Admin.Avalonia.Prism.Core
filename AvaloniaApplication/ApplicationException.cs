using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace AvaloniaApplication;

public partial class App
{
    private void RegisterGlobalExceptionHandler()
    {
        // UI调度器异常（界面同步代码报错）
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        
        // 未观察的Task异常（async void、未await任务崩溃）
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // App全局未捕获异常（兜底）
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    /// <summary>
    /// 异步Task后台未捕获异常
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved(); // 标记异常已处理，防止进程终止
        var ex = e.Exception.InnerException ?? e.Exception;
        ShowExceptionDialog("后台异步任务出错", ex);
    }

    /// <summary>
    /// UI线程同步异常
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDispatcherUnhandledException(object sender, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true; // 阻止程序直接崩溃
        var ex = e.Exception;
        ShowExceptionDialog("界面执行出错", ex);
    }
    
    /// <summary>
    /// 进程全局兜底（致命异常）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (ex != null)
        {
            ShowExceptionDialog("程序发生致命错误，即将退出", ex);
        }
        // e.IsTerminating = true 代表进程马上退出，无法挽回
    }
    
    /// <summary>弹窗展示错误（阻塞UI，使用MessageBox/Avalonia弹窗）</summary>
    private async void ShowExceptionDialog(string title, Exception ex)
    {
        // 获取主窗口
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWin = desktop.MainWindow;
            if (mainWin != null)
            {
                var msg = $"【{title}】\n{ex.Message}\n\n堆栈信息：\n{ex.StackTrace}";
               
            }
        }

        // 日志写入文件
        // LogHelper.WriteError(ex);
    }
}