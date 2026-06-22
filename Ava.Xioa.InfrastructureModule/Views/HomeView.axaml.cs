using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: AvaRouter.HomeView, region: AppRegions.MainRegion, typeof(IHomeServices),zIndex: 9998)]
public partial class HomeView : UserControl
{
    public HomeView(IToastsService toastsService)
    {
        InitializeComponent();
    }
}