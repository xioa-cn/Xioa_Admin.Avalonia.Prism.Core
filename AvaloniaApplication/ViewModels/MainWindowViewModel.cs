using System.Linq;
using System.Runtime.InteropServices;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Infrastructure.Models.Models.ThemesModels;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace AvaloniaApplication.ViewModels;

[PrismVm(typeof(MainWindowViewModel))]
public partial class MainWindowViewModel : ReactiveObject, IInitializedable
{
    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    [ObservableBindProperty] private SukiBackgroundStyleDesc _backgroundStyle;

    [ObservableBindProperty] private bool _transitionsEnabled;

    [ObservableBindProperty] private string _customShaderFile = null;

    [ObservableBindProperty] private bool _animationsEnabled = false;

    private readonly IThemesServices _themesServices;

    private readonly ISystemThemesInformationRepository _systemThemesInformationRepository;

    public MainWindowViewModel(ISukiToastManager toastManager, ISukiDialogManager dialogManager,
        IThemesServices themesServices, ISystemThemesInformationRepository systemThemesInformationRepository)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _themesServices = themesServices;
        _systemThemesInformationRepository = systemThemesInformationRepository;
        _themesServices.CustomBackgroundStyleChanged += value => CustomShaderFile = value;
        _themesServices.AnimationsEnabledChanged += value => AnimationsEnabled = (bool)value!;
        _themesServices.BackgroundTransitionsChanged += value => TransitionsEnabled = (bool)value!;
        _themesServices.BackgroundStyleChanged += value => BackgroundStyle = (SukiBackgroundStyleDesc)value!;
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        _themesServices.BackgroundAnimations = value;
    }

    public bool ApplicationLifetime => IsDesktopPlatform();

    /// <summary>
    /// 判断是否为桌面平台（Windows/macOS/Linux/BSD）
    /// </summary>
    private static bool IsDesktopPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
               || RuntimeInformation.IsOSPlatform(OSPlatform.Create("macOS"))
               || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
               || RuntimeInformation.IsOSPlatform(OSPlatform.Create("BSD"));
    }

    partial void OnBackgroundStyleChanged(SukiBackgroundStyleDesc value)
    {
        _themesServices.BackgroundStyle = value;
    }

    public void Initialized()
    {
        var findLastUseThemeInfo = this._systemThemesInformationRepository.DbSet.FirstOrDefault();
        _themesServices.SetThemesInformationRepository(_systemThemesInformationRepository);
        if (findLastUseThemeInfo == null) return;
        BackgroundStyle = SukiBackgroundStyleDesc.SukiBackgroundStyleDescs[findLastUseThemeInfo.BackgroundStyleKey];
        AnimationsEnabled = findLastUseThemeInfo.Animation;
        _themesServices.BackgroundAnimations = findLastUseThemeInfo.Animation;
        _themesServices.IsLightTheme = findLastUseThemeInfo.IsLightTheme;
        _themesServices.ChangeColorTheme(findLastUseThemeInfo.ColorThemeDisplayName);
        if (!string.IsNullOrEmpty(findLastUseThemeInfo.BackgroundEffectKey))
        {
            _themesServices.ChangeBackgroundEffect(findLastUseThemeInfo.BackgroundEffectKey);
        }
    }
}