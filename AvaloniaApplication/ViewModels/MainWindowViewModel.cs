using System;
using System.Linq;
using System.Runtime.InteropServices;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Microsoft.EntityFrameworkCore;
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

    public async void Initialized()
    {
        try
        {
            var findLastUseThemeInfo = await this._systemThemesInformationRepository.DbSet.FirstOrDefaultAsync();
            ThemesServices.SetThemesInformationRepository(_systemThemesInformationRepository);
            if (findLastUseThemeInfo == null) return;
           
            ThemesServices.BackgroundStyle = ThemesServices.AvailableBackgroundStyles[findLastUseThemeInfo.BackgroundStyleKey];
            ThemesServices.BackgroundAnimations = true;
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
    }
}
