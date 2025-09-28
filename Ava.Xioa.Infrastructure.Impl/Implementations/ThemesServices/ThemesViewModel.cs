using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Models.Models.ThemesModels;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Avalonia.Collections;
using Avalonia.Styling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using SukiUI;
using SukiUI.Models;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.ThemesServices;

[PrismViewModel(typeof(IThemesServices), ServiceLifetime.Scoped)]
public partial class ThemesViewModel : EventEnabledViewModelObject, IThemesServices
{
    [ObservableBindProperty] private bool _backgroundAnimations;
    [ObservableBindProperty] private bool _backgroundTransitions;
    [ObservableBindProperty] private bool _isLightTheme;
    [ObservableBindProperty] private SukiBackgroundStyleDesc _backgroundStyle;

    public IAvaloniaReadOnlyList<SukiColorTheme> AvailableColors { get; }
    public IAvaloniaReadOnlyList<SukiBackgroundStyleDesc> AvailableBackgroundStyles { get; }

    private readonly SukiTheme _theme = SukiTheme.GetInstance();

    private ISystemThemesInformationRepository? ThemesInformationRepository { get; set; }

    public void SetThemesInformationRepository(ISystemThemesInformationRepository repository)
    {
        this.ThemesInformationRepository = repository;
    }

    private readonly Debouncer _debouncer;

    public ThemesViewModel(IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
        AvailableBackgroundStyles =
            new AvaloniaList<SukiBackgroundStyleDesc>(SukiBackgroundStyleDesc.SukiBackgroundStyleDescs);

        AvailableColors = _theme.ColorThemes;

        LightThemeChangedCommand = new RelayCommand<bool?>(ChangeLightTheme);
        BackgroundEffectCommand = new AsyncRelayCommand(ChangeBackgroundEffect);
        AnimationsEnabledCommand = new RelayCommand(AnimationsEnabled);
        SwitchToColorThemeCommand = new RelayCommand<SukiColorTheme>(SwitchToColorTheme);

        _debouncer = new Debouncer(1000);
    }

    partial void OnBackgroundStyleChanged(SukiBackgroundStyleDesc value)
    {
        BackgroundStyleChanged?.Invoke(value);
    }

    partial void OnIsLightThemeChanged(bool value)
    {
        ChangeLightTheme(value);
    }

    partial void OnBackgroundAnimationsChanged(bool value)
    {
        AnimationsEnabled(value);
    }

    partial void OnBackgroundTransitionsChanged(bool value)
    {
        BackgroundTransitionsChanged?.Invoke(value);
    }

    private string? _colorThemeDisplayName;

    private void SwitchToColorTheme(SukiColorTheme? obj)
    {
        if (obj is null) return;

        _theme.ChangeColorTheme(obj);
        var color = obj.PrimaryBrush.ToString()?.Replace("ff", "");

        this.PublishEvent<ThemeChangedEvent, TokenKeyPubSubEvent<string>>(
            new TokenKeyPubSubEvent<string>("SystemColor", color ?? string.Empty));
        _colorThemeDisplayName = obj.DisplayName;
        _debouncer.DebounceAsync(async () => { await SaveThemeInformation(); });
    }

    private bool _animationsEnabled = false;

    private void AnimationsEnabled()
    {
        _animationsEnabled = !_animationsEnabled;
        AnimationsEnabledChanged?.Invoke(_animationsEnabled);
        _debouncer.DebounceAsync(async () => { await SaveThemeInformation(); });
    }

    private void AnimationsEnabled(bool value)
    {
        AnimationsEnabledChanged?.Invoke(value);
        _animationsEnabled = value;
        _debouncer.DebounceAsync(async () => { await SaveThemeInformation(); });
    }

    private bool _backgroundEffect = true;
    private bool _backgroundEffectChangeAnimations = false;

    private string? _backgroundEffectKey;

    public void SetPrivateBackgroundEffectKey(string key)
    {
        this._backgroundEffectKey = key;
    }

    private async Task ChangeBackgroundEffect()
    {
        if (_backgroundEffect && !BackgroundAnimations)
        {
            BackgroundAnimations = true;
            _backgroundEffectChangeAnimations = true;
        }
        else if (BackgroundAnimations && _backgroundEffectChangeAnimations)
        {
            BackgroundAnimations = false;
            _backgroundEffectChangeAnimations = false;
        }
        else
        {
            BackgroundAnimations = false;
            await Task.Delay(100);
            BackgroundAnimations = true;
        }

        _backgroundEffectKey = _backgroundEffectKey == null ? "Space" : null;
        CustomBackgroundStyleChanged?.Invoke(_backgroundEffectKey);
        _backgroundEffect = !_backgroundEffect;
        _debouncer.DebounceAsync(async () => { await SaveThemeInformation(); });
    }

    public void ChangeBackgroundEffect(string effectKey)
    {
        if (_backgroundEffect && !BackgroundAnimations)
        {
            BackgroundAnimations = true;
            _backgroundEffectChangeAnimations = true;
        }
        else if (BackgroundAnimations && _backgroundEffectChangeAnimations)
        {
            BackgroundAnimations = false;
            _backgroundEffectChangeAnimations = false;
        }

        _backgroundEffectKey = effectKey;
        _backgroundEffect = true;
        CustomBackgroundStyleChanged?.Invoke(effectKey);
    }

    private void ChangeLightTheme(bool? obj)
    {
        if (obj is null) return;
        _theme.ChangeBaseTheme((bool)obj ? ThemeVariant.Light : ThemeVariant.Dark);
        _debouncer.DebounceAsync(async () => { await SaveThemeInformation(); });
    }

    public void ChangeColorTheme(string displayName)
    {
        var find =
            AvailableColors.FirstOrDefault(item => item.DisplayName == displayName);

        if (find is not null)
        {
            SwitchToColorTheme(find);
        }
    }

    public Action<bool?>? AnimationsEnabledChanged { get; set; }
    public Action<string?>? CustomBackgroundStyleChanged { get; set; }
    public Action<bool?>? BackgroundTransitionsChanged { get; set; }
    public Action<SukiBackgroundStyleDesc?>? BackgroundStyleChanged { get; set; }
    public ICommand LightThemeChangedCommand { get; }
    public ICommand BackgroundEffectCommand { get; }
    public ICommand AnimationsEnabledCommand { get; }
    public ICommand SwitchToColorThemeCommand { get; }


    private async Task SaveThemeInformation()
    {
        if (this.ThemesInformationRepository is null)
        {
            return;
        }

        var findThemeInfo = await this.ThemesInformationRepository.DbSet.FirstOrDefaultAsync(item => item.Id == 1);

        if (findThemeInfo is null)
        {
            findThemeInfo = new SystemThemesInformation();

            findThemeInfo.Id = 1;
            findThemeInfo.BackgroundStyleKey = this.BackgroundStyle?.Key ?? 0;
            findThemeInfo.IsLightTheme = this.IsLightTheme;
            findThemeInfo.Animation = this.BackgroundAnimations;
            findThemeInfo.ColorThemeDisplayName = _colorThemeDisplayName ?? "Orange";
            findThemeInfo.BackgroundEffectKey = _backgroundEffectKey;


            this.ThemesInformationRepository.DbSet.Add(findThemeInfo);
        }
        else
        {
            findThemeInfo.BackgroundStyleKey = this.BackgroundStyle?.Key ?? 0;
            findThemeInfo.IsLightTheme = this.IsLightTheme;
            findThemeInfo.Animation = this.BackgroundAnimations;
            findThemeInfo.ColorThemeDisplayName = _colorThemeDisplayName ?? "Orange";
            findThemeInfo.BackgroundEffectKey = _backgroundEffectKey;
        }

        await this.ThemesInformationRepository.SaveChangesAsync();
    }
}