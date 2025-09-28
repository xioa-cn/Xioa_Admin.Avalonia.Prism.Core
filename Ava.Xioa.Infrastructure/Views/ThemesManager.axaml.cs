using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Avalonia.Controls;

namespace Ava.Xioa.Infrastructure.Views;

[RegisterForNavigation(navigationName: nameof(ThemesManager), region: AppRegions.MainRegion)]
public partial class ThemesManager : UserControl
{
    public ThemesManager(IThemesServices themesServices)
    {
        this.DataContext = themesServices;
        InitializeComponent();
    }
}