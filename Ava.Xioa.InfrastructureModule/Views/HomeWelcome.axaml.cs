using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Connectlayer.Global;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: AvaRouter.HomeWelcome, region: AppRegions.HomeRegion, zIndex: 8999)]
public partial class HomeWelcome : UserControl
{
    public HomeWelcome()
    {
        InitializeComponent();
    }
}