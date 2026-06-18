using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: AvaRouter.ThemesManager, region: AppRegions.HomeRegion)]
public partial class ThemesManager : UserControl
{
    public ThemesManager(IThemesServices themesServices)
    {
        this.DataContext = themesServices;
        InitializeComponent();
    }
}