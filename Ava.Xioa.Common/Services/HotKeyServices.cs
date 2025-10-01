using System;
using System.Collections.Generic;
using Ava.Xioa.Common.Attributes;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Prism.Commands;

namespace Ava.Xioa.Common.Services;

[PrismService(typeof(HotKeyServices), ServiceLifetime.Singleton)]
public class HotKeyServices(IApplicationLifetime liftime)
{
    private readonly Dictionary<string, KeyBinding> _hotKeyDictionary =
        new Dictionary<string, KeyBinding>();

    public void SetPageHotKey(KeyGesture keyGesture, Action action, string hotKeyName)
    {
        if (_hotKeyDictionary.TryGetValue(hotKeyName, out _))
        {
            throw new Exception("HotKey already exists");
        }

        KeyBinding keyBinding = new KeyBinding()
        {
            Gesture = keyGesture,
            Command = new DelegateCommand(action)
        };
        if (liftime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            desktop.MainWindow.KeyBindings.Add(keyBinding);
        }
        else if (liftime is ISingleViewApplicationLifetime { MainView: not null } singleView)
        {
            singleView.MainView.KeyBindings.Add(keyBinding);
        }
        else
        {
            throw new Exception("Cannot set hotkey");
        }

        _hotKeyDictionary.Add(hotKeyName, keyBinding);
    }

    private void RemoveHotKey(KeyBinding keyBinding)
    {
        if (liftime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            desktop.MainWindow.KeyBindings.Remove(keyBinding);
        }
        else if (liftime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView?.KeyBindings.Remove(keyBinding);
        }
    }

    public void RemoveHotKey(string hotKeyName)
    {
        if (!_hotKeyDictionary.TryGetValue(hotKeyName, out var keyBinding)) return;
        RemoveHotKey(keyBinding);
        _hotKeyDictionary.Remove(hotKeyName, out _);
    }
}