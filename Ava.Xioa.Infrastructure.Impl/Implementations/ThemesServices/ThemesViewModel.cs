using System;
using System.Windows.Input;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using SukiUI;
using SukiUI.Models;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.ThemesServices;

[PrismVm(typeof(IThemesServices), ServiceLifetime.Singleton)]
public partial class ThemesViewModel : EventEnabledViewModelObject, IThemesServices
{
    private readonly ToastsService _toastsService;
    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }

    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    public ThemesViewModel(ToastsService toastsService, IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
        _toastsService = toastsService;
        AvailableColors = _theme.ColorThemes;

        LightThemeChangedCommand = new RelayCommand<bool?>(ChangeLightTheme);
        BackgroundEffectCommand = new RelayCommand(ChangeBackgroundEffect);
        AnimationsEnabledCommand = new RelayCommand(AnimationsEnabled);
        SwitchToColorThemeCommand = new RelayCommand<SukiColorTheme>(SwitchToColorTheme);
    }

    private void SwitchToColorTheme(SukiColorTheme? obj)
    {
        if (obj is null) return;

        _theme.ChangeColorTheme(obj);
        var color = obj.PrimaryBrush.ToString().Replace("ff", "");

        this.PublishEvent<ThemeChangedEvent, TokenKeyPubSubEvent<string>>(
            new TokenKeyPubSubEvent<string>("SystemColor", color));

        _toastsService.ShowToast(NotificationType.Success, "切换主题", obj.DisplayName);
    }

    private bool _animationsEnabled = false;

    private void AnimationsEnabled()
    {
        AnimationsEnabledChanged?.Invoke(_animationsEnabled);
        _animationsEnabled = !_animationsEnabled;
    }

    private bool _backgroundEffect = true;

    private void ChangeBackgroundEffect()
    {
        CustomBackgroundStyleChanged?.Invoke(_backgroundEffect ? "Space" : null);
        _backgroundEffect = !_backgroundEffect;
    }

    private void ChangeLightTheme(bool? obj)
    {
        if (obj is null) return;
        _theme.ChangeBaseTheme((bool)obj ? ThemeVariant.Light : ThemeVariant.Dark);
    }

    public Action<bool?>? AnimationsEnabledChanged { get; set; }
    public Action<string?>? CustomBackgroundStyleChanged { get; set; }
    public ICommand LightThemeChangedCommand { get; }
    public ICommand BackgroundEffectCommand { get; }
    public ICommand AnimationsEnabledCommand { get; }
    public ICommand SwitchToColorThemeCommand { get; }
}