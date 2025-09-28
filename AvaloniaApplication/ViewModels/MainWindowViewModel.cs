using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
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

    [ObservableBindProperty] private SukiBackgroundStyle _backgroundStyle = SukiBackgroundStyle.Bubble;

    [ObservableBindProperty] private bool _transitionsEnabled;

    [ObservableBindProperty] private string _customShaderFile = null;

    [ObservableBindProperty] private bool _animationsEnabled = false;

    private readonly IThemesServices _themesServices;

    public MainWindowViewModel(ISukiToastManager toastManager, ISukiDialogManager dialogManager,
        IThemesServices mainViewViewModel)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _themesServices = mainViewViewModel;

        mainViewViewModel.CustomBackgroundStyleChanged += value => CustomShaderFile = value;
        mainViewViewModel.AnimationsEnabledChanged += value => AnimationsEnabled = (bool)value!;
    }
}