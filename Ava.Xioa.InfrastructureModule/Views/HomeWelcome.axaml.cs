using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(HomeWelcome), region: AppRegions.HomeRegion, zIndex: 8999)]
public partial class HomeWelcome : UserControl
{
    public HomeWelcome()
    {
        InitializeComponent();
    }
}