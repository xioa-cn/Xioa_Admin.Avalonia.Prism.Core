
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Prism.Ioc;
using Prism.Mvvm;

namespace Prism.Dialogs;

public class DialogHost : Grid
{
    public const string DefaultHostKey = "Default";

    public static readonly StyledProperty<string> HostKeyProperty =
        AvaloniaProperty.Register<DialogHost, string>(nameof(HostKey), DefaultHostKey);

    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<DialogHost, IBrush?>(nameof(OverlayBrush), new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)));

    public static readonly StyledProperty<bool> CloseOnOverlayClickProperty =
        AvaloniaProperty.Register<DialogHost, bool>(nameof(CloseOnOverlayClick), true);

    public static readonly StyledProperty<bool> CloseOnEscapeProperty =
        AvaloniaProperty.Register<DialogHost, bool>(nameof(CloseOnEscape), true);

    private static readonly Dictionary<string, List<WeakReference<DialogHost>>> Hosts = new(StringComparer.Ordinal);
    private static readonly object HostsSyncRoot = new();
    private readonly Grid _overlay;
    private readonly Stack<DialogHostEntry> _dialogStack = new();
    private bool _isRegistered;

    public DialogHost()
    {
        _overlay = new Grid
        {
            IsVisible = false,
            Focusable = true
        };

        _overlay.PointerPressed += OnOverlayPointerPressed;
        _overlay.KeyDown += OnOverlayKeyDown;
        Children.Add(_overlay);
    }

    public string HostKey
    {
        get => GetValue(HostKeyProperty);
        set => SetValue(HostKeyProperty, value);
    }

    public IBrush? OverlayBrush
    {
        get => GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    public bool CloseOnOverlayClick
    {
        get => GetValue(CloseOnOverlayClickProperty);
        set => SetValue(CloseOnOverlayClickProperty, value);
    }

    public bool CloseOnEscape
    {
        get => GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RegisterHost();
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        ClearDialogs();
        UnregisterHost();
        base.OnDetachedFromVisualTree(e);
    }

    public static bool TryGetHost(string hostKey, out DialogHost host)
    {
        lock (HostsSyncRoot)
        {
            if (!Hosts.TryGetValue(hostKey, out var hosts))
            {
                host = null!;
                return false;
            }

            for (var index = hosts.Count - 1; index >= 0; index--)
            {
                if (!hosts[index].TryGetTarget(out var candidate) || !candidate._isRegistered)
                {
                    hosts.RemoveAt(index);
                    continue;
                }

                host = candidate;
                return true;
            }

            Hosts.Remove(hostKey);
        }

        host = null!;
        return false;
    }

    public void Show(Control dialogContent, Func<Task> requestClose, DialogOptions options)
    {
        dialogContent.HorizontalAlignment = HorizontalAlignment.Center;
        dialogContent.VerticalAlignment = VerticalAlignment.Center;
        dialogContent.PointerPressed += OnDialogContentPointerPressed;

        EnsureOverlayOnTop();
        _overlay.Children.Add(dialogContent);
        _dialogStack.Push(new DialogHostEntry(dialogContent, requestClose, options));
        UpdateOverlayState();
        _overlay.Focus();
    }

    public void Close(Control dialogContent)
    {
        RemoveDialogFromStack(dialogContent);
        _overlay.Children.Remove(dialogContent);
        dialogContent.PointerPressed -= OnDialogContentPointerPressed;
        if (_overlay.Children.Count == 0)
        {
            _overlay.IsVisible = false;
        }
        else
        {
            UpdateOverlayState();
            _overlay.Focus();
        }
    }

    private void RegisterHost()
    {
        lock (HostsSyncRoot)
        {
            if (!Hosts.TryGetValue(HostKey, out var hosts))
            {
                hosts = new List<WeakReference<DialogHost>>();
                Hosts[HostKey] = hosts;
            }

            PruneHosts(hosts);
            if (!ContainsHost(hosts, this))
            {
                hosts.Add(new WeakReference<DialogHost>(this));
            }

            _isRegistered = true;
        }
    }

    private void UnregisterHost()
    {
        lock (HostsSyncRoot)
        {
            _isRegistered = false;
            if (!Hosts.TryGetValue(HostKey, out var hosts))
            {
                return;
            }

            for (var index = hosts.Count - 1; index >= 0; index--)
            {
                if (!hosts[index].TryGetTarget(out var existing) || ReferenceEquals(existing, this))
                {
                    hosts.RemoveAt(index);
                }
            }

            if (hosts.Count == 0)
            {
                Hosts.Remove(HostKey);
            }
        }
    }

    private static void PruneHosts(List<WeakReference<DialogHost>> hosts)
    {
        for (var index = hosts.Count - 1; index >= 0; index--)
        {
            if (!hosts[index].TryGetTarget(out var host) || !host._isRegistered)
            {
                hosts.RemoveAt(index);
            }
        }
    }

    private static bool ContainsHost(List<WeakReference<DialogHost>> hosts, DialogHost host)
    {
        foreach (var reference in hosts)
        {
            if (reference.TryGetTarget(out var existing) && ReferenceEquals(existing, host))
            {
                return true;
            }
        }

        return false;
    }

    private async Task RequestCloseTopAsync()
    {
        while (_dialogStack.Count > 0)
        {
            var entry = _dialogStack.Peek();
            if (_overlay.Children.Contains(entry.Content))
            {
                await entry.RequestClose().ConfigureAwait(true);
                return;
            }

            _dialogStack.Pop();
        }
    }

    private void RemoveDialogFromStack(Control dialogContent)
    {
        if (_dialogStack.Count == 0)
        {
            return;
        }

        var entries = _dialogStack.ToArray();
        _dialogStack.Clear();
        for (var index = entries.Length - 1; index >= 0; index--)
        {
            if (!ReferenceEquals(entries[index].Content, dialogContent))
            {
                _dialogStack.Push(entries[index]);
            }
        }
    }

    private void ClearDialogs()
    {
        while (_dialogStack.Count > 0)
        {
            var entry = _dialogStack.Pop();
            entry.Content.PointerPressed -= OnDialogContentPointerPressed;
        }

        _overlay.Children.Clear();
        _overlay.IsVisible = false;
    }

    private async void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _overlay) && TryGetActiveEntry(out var entry) && entry.Options.CloseOnOverlayClick && CloseOnOverlayClick)
        {
            e.Handled = true;
            await RequestCloseTopAsync().ConfigureAwait(true);
        }
    }

    private static void OnDialogContentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private async void OnOverlayKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && TryGetActiveEntry(out var entry) && entry.Options.CloseOnEscape && CloseOnEscape)
        {
            e.Handled = true;
            await RequestCloseTopAsync().ConfigureAwait(true);
        }
    }

    private void EnsureOverlayOnTop()
    {
        if (Children.Count > 0 && ReferenceEquals(Children[Children.Count - 1], _overlay))
        {
            return;
        }

        Children.Remove(_overlay);
        Children.Add(_overlay);
    }

    private bool TryGetActiveEntry(out DialogHostEntry entry)
    {
        while (_dialogStack.Count > 0)
        {
            entry = _dialogStack.Peek();
            if (_overlay.Children.Contains(entry.Content))
            {
                return true;
            }

            _dialogStack.Pop();
        }

        entry = null!;
        return false;
    }

    private void UpdateOverlayState()
    {
        _overlay.IsVisible = _overlay.Children.Count > 0;
        _overlay.Background = TryGetActiveEntry(out var entry)
            ? entry.Options.OverlayBrush ?? OverlayBrush
            : OverlayBrush;
    }

    private sealed class DialogHostEntry
    {
        public DialogHostEntry(Control content, Func<Task> requestClose, DialogOptions options)
        {
            Content = content;
            RequestClose = requestClose;
            Options = options;
        }

        public Control Content { get; }

        public Func<Task> RequestClose { get; }

        public DialogOptions Options { get; }
    }
}