using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
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

    [ObservableBindProperty] private bool _animationsEnabled;

    private readonly MainViewViewModel _mainViewViewModel;

    public MainWindowViewModel(ISukiToastManager toastManager, ISukiDialogManager dialogManager,
        MainViewViewModel mainViewViewModel)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _mainViewViewModel = mainViewViewModel;

        mainViewViewModel.CustomBackgroundStyleChanged += value => CustomShaderFile = value;
        mainViewViewModel.AnimationsEnabledChanged += value => AnimationsEnabled = (bool)value!;
    }
}