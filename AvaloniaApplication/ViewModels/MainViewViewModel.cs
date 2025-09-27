using System;
using System.Windows.Input;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using AvaloniaApplication.PubSubEvents;
using Prism.Events;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;

namespace AvaloniaApplication.ViewModels;

[PrismVm(typeof(MainViewViewModel))]
public class MainViewViewModel : EventEnabledViewModelObject
{
    public ICommand ChangeSystemColorCommand { get; }
    public ICommand LightThemeChangedCommand { get; }
    public ICommand BackgroundEffectCommand { get; }
    public ICommand AnimationsEnabledCommand { get; }
    public ICommand SwitchToColorThemeCommand { get; }

    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

    private readonly SukiTheme _theme = SukiTheme.GetInstance();
    public Action<bool?>? AnimationsEnabledChanged { get; set; }
    public Action<string?>? CustomBackgroundStyleChanged { get; set; }

    private readonly ToastsService _toastsService;

    public MainViewViewModel(IEventAggregator eventAggregator, ToastsService toastsService) : base(eventAggregator)
    {
        _toastsService = toastsService;
        AvailableColors = _theme.ColorThemes;
        var t = AvailableColors[0];
        ChangeSystemColorCommand = new RelayCommand(ChangeSystemColor);
        LightThemeChangedCommand = new RelayCommand<bool?>(ChangeLightTheme);
        BackgroundEffectCommand = new RelayCommand<bool?>(ChangeBackgroundEffect);
        AnimationsEnabledCommand = new RelayCommand<bool?>(AnimationsEnabled);
        SwitchToColorThemeCommand = new RelayCommand<SukiColorTheme>(SwitchToColorTheme);
    }

    private void SwitchToColorTheme(SukiColorTheme? obj)
    {
        if (obj is null) return;

        _theme.ChangeColorTheme(obj);
        var color = obj.PrimaryBrush.ToString().Replace("ff","");
        
        this.PublishEvent<ThemeChangedEvent, TokenKeyPubSubEvent<string>>(
            new TokenKeyPubSubEvent<string>("SystemColor", color));
        
        _toastsService.ShowToast(NotificationType.Success,"切换主题",obj.DisplayName);
    }

    private void AnimationsEnabled(bool? obj)
    {
        if (obj is null) return;

        AnimationsEnabledChanged?.Invoke(obj);
    }

    private void ChangeBackgroundEffect(bool? value)
    {
        if (value is null) return;

        CustomBackgroundStyleChanged?.Invoke((bool)value ? "Space" : null);
    }

    private void ChangeLightTheme(bool? value)
    {
        if (value is null) return;
        _theme.ChangeBaseTheme((bool)value ? ThemeVariant.Light : ThemeVariant.Dark);
    }

    private void ChangeSystemColor()
    {
        // this.PublishEvent<ThemeChangedEvent, TokenKeyPubSubEvent<string>>(
        //     new TokenKeyPubSubEvent<string>("SystemColor", RandomColor.GenerateRandomColor()));
    }
}