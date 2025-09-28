using System.Runtime.InteropServices;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
using Ava.Xioa.Infrastructure.Models.Models.ThemesModels;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;

namespace AvaloniaApplication.ViewModels;

[PrismVm(typeof(MainWindowViewModel))]
public partial class MainWindowViewModel : ReactiveObject
{
    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    [ObservableBindProperty]
    private SukiBackgroundStyleDesc _backgroundStyle = SukiBackgroundStyleDesc.SukiBackgroundStyleDescs[0];

    [ObservableBindProperty] private bool _transitionsEnabled;

    [ObservableBindProperty] private string _customShaderFile = null;

    [ObservableBindProperty] private bool _animationsEnabled = false;

    private readonly IThemesServices _themesServices;

    public MainWindowViewModel(ISukiToastManager toastManager, ISukiDialogManager dialogManager,
        IThemesServices themesServices)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _themesServices = themesServices;
        _themesServices.BackgroundStyle = _backgroundStyle;
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
}