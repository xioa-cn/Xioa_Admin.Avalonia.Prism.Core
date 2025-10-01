using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(HomeView), region: AppRegions.MainRegion, zIndex: 9998)]
public partial class HomeView : UserControl
{
    public HomeView(IHomeServices homeServices, ToastsService toastsService)
    {
        this.DataContext = homeServices;
        InitializeComponent();
        // 注册一次性Loaded事件（执行后自动移除）
        this.OnceExecutedLoaded(() =>
        {
            toastsService.ShowToast(
                NotificationType.Success, 
                "Hello",
                GetDayText.ApplicationSayHello(GlobalLoginInformation.Instance.UserName)
            );
        });
    }
}