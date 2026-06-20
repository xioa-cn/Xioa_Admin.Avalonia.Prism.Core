using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Prism.Ioc;
using Prism.Mvvm;

namespace Prism.Dialogs;

public class DialogService : IDialogService
{
    private static readonly DialogCloseListener EmptyCloseListener = new(_ => { });
    private readonly IContainerProvider _container;

    public DialogService(IContainerProvider container)
    {
        _container = container;
    }

    public void Show(string name)
    {
        Show(DialogHost.DefaultHostKey, name, null, null);
    }

    public void Show(string hostKey, string name)
    {
        Show(hostKey, name, null, null);
    }

    public void Show(string name, IDialogParameters parameters)
    {
        Show(DialogHost.DefaultHostKey, name, parameters, null);
    }

    public void Show(string hostKey, string name, IDialogParameters parameters)
    {
        Show(hostKey, name, parameters, null);
    }

    public void Show(string name, Action<IDialogResult> callback)
    {
        Show(DialogHost.DefaultHostKey, name, null, callback);
    }

    public void Show(string hostKey, string name, Action<IDialogResult> callback)
    {
        Show(hostKey, name, null, callback);
    }

    public void Show(string name, IDialogParameters? parameters, Action<IDialogResult>? callback)
    {
        Show(DialogHost.DefaultHostKey, name, parameters, callback);
    }

    public void Show(string hostKey, string name, IDialogParameters? parameters, Action<IDialogResult>? callback)
    {
        ShowCore(hostKey, name, parameters, callback, false);
    }

    public void ShowDialog(string name)
    {
        ShowDialog(DialogHost.DefaultHostKey, name, null, null);
    }

    public void ShowDialog(string hostKey, string name)
    {
        ShowDialog(hostKey, name, null, null);
    }

    public void ShowDialog(string name, IDialogParameters parameters)
    {
        ShowDialog(DialogHost.DefaultHostKey, name, parameters, null);
    }

    public void ShowDialog(string hostKey, string name, IDialogParameters parameters)
    {
        ShowDialog(hostKey, name, parameters, null);
    }

    public void ShowDialog(string name, Action<IDialogResult> callback)
    {
        ShowDialog(DialogHost.DefaultHostKey, name, null, callback);
    }

    public void ShowDialog(string hostKey, string name, Action<IDialogResult> callback)
    {
        ShowDialog(hostKey, name, null, callback);
    }

    public void ShowDialog(string name, IDialogParameters? parameters, Action<IDialogResult>? callback)
    {
        ShowDialog(DialogHost.DefaultHostKey, name, parameters, callback);
    }

    public void ShowDialog(string hostKey, string name, IDialogParameters? parameters, Action<IDialogResult>? callback)
    {
        ShowCore(hostKey, name, parameters, callback, true);
    }

    private void ShowCore(string hostKey, string name, IDialogParameters? parameters, Action<IDialogResult>? callback, bool modal)
    {
        var view = _container.Resolve(typeof(object), name);
        if (view is Control control)
        {
            using (ViewModelLocationProvider.UseScopedViewModelFactory((view, type) => _container.Resolve(type)))
            {
                control.DataContext ??= ViewModelLocationProvider.AutoWireViewModelChanged(control);
            }
        }

        var options = DialogOptions.From(parameters);
        var dialogAware = (view as Control)?.DataContext as IDialogAware ?? view as IDialogAware;
        dialogAware?.OnDialogOpened(parameters ?? new DialogParameters());

        IDialogResult result = new DialogResult(ButtonResult.None);
        Func<Task>? close = null;
        Action<IDialogResult>? requestCloseHandler = null;
        var subscriptionsCleaned = false;
        var completed = false;

        void CleanupSubscriptions()
        {
            if (subscriptionsCleaned)
            {
                return;
            }

            subscriptionsCleaned = true;
            if (dialogAware is null)
            {
                return;
            }

            if (requestCloseHandler is not null)
            {
                dialogAware.RequestClose -= requestCloseHandler;
            }

            dialogAware.DialogCloseListener = EmptyCloseListener;
        }

        void RequestClose(IDialogResult requestedResult)
        {
            _ = RequestCloseAsync(requestedResult);
        }

        async Task RequestCloseAsync(IDialogResult requestedResult)
        {
            if (completed)
            {
                return;
            }

            if (!await CanCloseDialogAsync(dialogAware).ConfigureAwait(true))
            {
                return;
            }

            result = requestedResult;
            if (close is not null)
            {
                await close().ConfigureAwait(true);
            }
        }

        if (dialogAware is not null)
        {
            requestCloseHandler = RequestClose;
            dialogAware.DialogCloseListener = new DialogCloseListener(RequestClose);
            dialogAware.RequestClose += requestCloseHandler;
        }

        if (TryShowOverlay(hostKey, view, dialogAware, callback, () => result, CleanupSubscriptions, options, parameters ?? new DialogParameters(), () => completed, () => completed = true, out close))
        {
            StartTimeout(options, () => completed ? Task.CompletedTask : RequestCloseAsync(new DialogResult(options.TimeoutResult)));
            return;
        }

        var dialogWindow = CreateDialogWindow(name, view);
        var window = dialogWindow as Window ?? throw new InvalidOperationException("The resolved dialog window must be an Avalonia Window.");
        ApplyWindowOptions(window, options);
        if (!string.IsNullOrWhiteSpace(dialogAware?.Title))
        {
            dialogWindow.Title = dialogAware.Title;
        }

        var windowCloseValidated = false;
        close = async () =>
        {
            if (completed)
            {
                return;
            }

            if (!windowCloseValidated)
            {
                await RunClosingAnimationAsync(options, new DialogAnimationContext(window, null, true, parameters ?? new DialogParameters())).ConfigureAwait(true);
            }

            windowCloseValidated = true;
            window.Close();
        };

        async Task<bool> ValidateAndCloseWindowAsync()
        {
            if (!await CanCloseDialogAsync(dialogAware).ConfigureAwait(true))
            {
                return false;
            }

            await close().ConfigureAwait(true);
            return true;
        }

        void OnWindowClosing(object? sender, WindowClosingEventArgs args)
        {
            if (windowCloseValidated)
            {
                return;
            }

            if (dialogAware is IAsyncDialogAware)
            {
                args.Cancel = true;
                _ = ValidateAndCloseWindowAsync();
                return;
            }

            if (dialogAware is not null && !dialogAware.CanCloseDialog())
            {
                args.Cancel = true;
                return;
            }

            args.Cancel = true;
            _ = ValidateAndCloseWindowAsync();
        }

        void OnWindowClosed(object? sender, EventArgs args)
        {
            window.Closing -= OnWindowClosing;
            window.Closed -= OnWindowClosed;
            window.Opened -= OnWindowOpened;
            completed = true;
            CompleteDialog(dialogAware, callback, result, CleanupSubscriptions);
        }

        async void OnWindowOpened(object? sender, EventArgs args)
        {
            await RunOpeningAnimationAsync(options, new DialogAnimationContext(window, null, true, parameters ?? new DialogParameters())).ConfigureAwait(true);
        }

        window.Opened += OnWindowOpened;
        window.Closing += OnWindowClosing;
        window.Closed += OnWindowClosed;

        if (modal)
        {
            var owner = GetOwnerWindow(hostKey, view);
            if (owner is not null)
            {
                window.ShowDialog(owner);
            }
            else
            {
                window.Show();
            }
        }
        else
        {
            window.Show();
        }

        StartTimeout(options, () => completed ? Task.CompletedTask : RequestCloseAsync(new DialogResult(options.TimeoutResult)));
    }

    private IDialogWindow CreateDialogWindow(string name, object view)
    {
        if (view is Window viewWindow)
        {
            return viewWindow is IDialogWindow dialogViewWindow
                ? dialogViewWindow
                : new DialogWindow
                {
                    Content = viewWindow.Content,
                    Title = viewWindow.Title
                };
        }

        IDialogWindow? window = null;
        if (_container.IsRegistered(typeof(IDialogWindow), name))
        {
            window = (IDialogWindow)_container.Resolve(typeof(IDialogWindow), name);
        }
        else if (_container.IsRegistered(typeof(IDialogWindow), "DefaultDialogWindow"))
        {
            window = (IDialogWindow)_container.Resolve(typeof(IDialogWindow), "DefaultDialogWindow");
        }
        else if (_container.IsRegistered(typeof(IDialogWindow)))
        {
            window = (IDialogWindow)_container.Resolve(typeof(IDialogWindow));
        }

        window ??= new DialogWindow();
        window.Content = view;
        return window;
    }

    private static bool TryShowOverlay(
        string hostKey,
        object view,
        IDialogAware? dialogAware,
        Action<IDialogResult>? callback,
        Func<IDialogResult> getResult,
        Action cleanup,
        DialogOptions options,
        IDialogParameters parameters,
        Func<bool> isCompleted,
        Action markCompleted,
        out Func<Task> close)
    {
        close = null!;

        if (!DialogHost.TryGetHost(hostKey, out var host) || view is not Control dialogContent)
        {
            return false;
        }

        var closed = false;
        close = async () =>
        {
            if (closed || isCompleted())
            {
                return;
            }

            if (!await CanCloseDialogAsync(dialogAware).ConfigureAwait(true))
            {
                return;
            }

            closed = true;
            await RunClosingAnimationAsync(options, new DialogAnimationContext(dialogContent, host, false, parameters)).ConfigureAwait(true);
            host.Close(dialogContent);
            markCompleted();
            CompleteDialog(dialogAware, callback, getResult(), cleanup);
        };

        host.Show(dialogContent, close, options);
        _ = RunOpeningAnimationAsync(options, new DialogAnimationContext(dialogContent, host, false, parameters));
        return true;
    }

    private static void StartTimeout(DialogOptions options, Func<Task> close)
    {
        if (options.Timeout is not { } timeout || timeout <= TimeSpan.Zero)
        {
            return;
        }

        _ = CloseAfterTimeoutAsync(timeout, close);
    }

    private static async Task CloseAfterTimeoutAsync(TimeSpan timeout, Func<Task> close)
    {
        await Task.Delay(timeout).ConfigureAwait(true);
        await close().ConfigureAwait(true);
    }

    private static async Task RunOpeningAnimationAsync(DialogOptions options, DialogAnimationContext context)
    {
        options.Opening?.Invoke(context);

        if (options.OpeningAsync is not null)
        {
            await options.OpeningAsync(context).ConfigureAwait(true);
        }

        var animation = options.GetAnimation();
        if (animation is not null)
        {
            await animation.OnOpeningAsync(context).ConfigureAwait(true);
        }
    }

    private static async Task RunClosingAnimationAsync(DialogOptions options, DialogAnimationContext context)
    {
        options.Closing?.Invoke(context);

        if (options.ClosingAsync is not null)
        {
            await options.ClosingAsync(context).ConfigureAwait(true);
        }

        var animation = options.GetAnimation();
        if (animation is not null)
        {
            await animation.OnClosingAsync(context).ConfigureAwait(true);
        }
    }

    private static async Task<bool> CanCloseDialogAsync(IDialogAware? dialogAware)
    {
        if (dialogAware is null)
        {
            return true;
        }

        if (dialogAware is IAsyncDialogAware asyncDialogAware)
        {
            return await asyncDialogAware.CanCloseDialogAsync().ConfigureAwait(true);
        }

        return dialogAware.CanCloseDialog();
    }

    private static void ApplyWindowOptions(Window window, DialogOptions options)
    {
        if (options.Width is { } width)
        {
            window.Width = width;
        }

        if (options.Height is { } height)
        {
            window.Height = height;
        }

        if (options.CanResize is { } canResize)
        {
            window.CanResize = canResize;
        }

        if (options.Topmost is { } topmost)
        {
            window.Topmost = topmost;
        }

        if (options.ShowInTaskbar is { } showInTaskbar)
        {
            window.ShowInTaskbar = showInTaskbar;
        }

        if (options.WindowStartupLocation is { } startupLocation)
        {
            window.WindowStartupLocation = startupLocation;
        }
    }

    private static void CompleteDialog(IDialogAware? dialogAware, Action<IDialogResult>? callback, IDialogResult result, Action? cleanup)
    {
        cleanup?.Invoke();
        dialogAware?.OnDialogClosed();
        callback?.Invoke(result);
    }

    private static Window? GetOwnerWindow(string hostKey, object view)
    {
        if (view is Control control && TopLevel.GetTopLevel(control) is Window viewWindow)
        {
            return viewWindow;
        }

        if (DialogHost.TryGetHost(hostKey, out var host) && TopLevel.GetTopLevel(host) is Window hostWindow)
        {
            return hostWindow;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.IsActive)
                {
                    return window;
                }
            }

            return desktop.MainWindow;
        }

        return null;
    }
}