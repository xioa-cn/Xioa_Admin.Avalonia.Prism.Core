using System;
using System.Linq;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Prism.Events;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace AvaloniaApplication.ViewModels;

[PrismViewModel(typeof(MainWindowViewModel))]
public partial class MainWindowViewModel : ReactiveObject, IInitializedable
{
    public ISukiDialogManager DialogManager { get; }

    private readonly ISystemThemesInformationRepository _systemThemesInformationRepository;

    public IThemesServices ThemesServices { get; }

    public IMainWindowServices MainWindowServices { get; }

    public IRegionManager RegionManager { get; set; }
    
    public IEventAggregator? EventAggregator { get; set; }

    private bool _themeInitialized;
    
    public MainWindowViewModel(
        IThemesServices themesServices, ISystemThemesInformationRepository systemThemesInformationRepository,
        IMainWindowServices mainWindowServices, IRegionManager regionManager, IEventAggregator eventAggregator, ISukiDialogManager dialogManager)
    {
        ThemesServices = themesServices;
        _systemThemesInformationRepository = systemThemesInformationRepository;
        MainWindowServices = mainWindowServices;
        RegionManager = regionManager;
        EventAggregator = eventAggregator;
        DialogManager = dialogManager;
    }

    public bool ApplicationLifetime => OperatingSystemUtil.IsDesktopPlatform();

    public void Initialized()
    {
        InitializeTheme();
    }

    public void InitializeTheme()
    {
        if (_themeInitialized)
        {
            return;
        }

        try
        {
            var findLastUseThemeInfo = _systemThemesInformationRepository.DbSet.FirstOrDefault();
            if (findLastUseThemeInfo == null) return;

            if (findLastUseThemeInfo.BackgroundStyleKey >= 0 &&
                findLastUseThemeInfo.BackgroundStyleKey < ThemesServices.AvailableBackgroundStyles.Count)
            {
                ThemesServices.BackgroundStyle =
                    ThemesServices.AvailableBackgroundStyles[findLastUseThemeInfo.BackgroundStyleKey];
            }

            ThemesServices.BackgroundAnimations = findLastUseThemeInfo.Animation;
            ThemesServices.IsLightTheme = findLastUseThemeInfo.IsLightTheme;
            ThemesServices.ChangeColorTheme(findLastUseThemeInfo.ColorThemeDisplayName);
            ThemesServices.FontFamily = findLastUseThemeInfo.FontFamily;
        
            if (!string.IsNullOrEmpty(findLastUseThemeInfo.BackgroundEffectKey))
            {
                ThemesServices.ChangeBackgroundEffect(findLastUseThemeInfo.BackgroundEffectKey);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            ThemesServices.SetThemesInformationRepository(_systemThemesInformationRepository);
            _themeInitialized = true;
        }
    }
}
