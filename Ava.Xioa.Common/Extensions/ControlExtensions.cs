using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ava.Xioa.Common.Extensions;

public static class ControlExtensions
{
    public static void OnceExecutedLoaded(this Control control, Action action)
    {
        EventHandler<RoutedEventArgs>? loadedHandler = null;
        loadedHandler = (sender, e) =>
        {
            action.Invoke();
            control.Loaded -= loadedHandler;
        };
        control.Loaded += loadedHandler;
    }
}