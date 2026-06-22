using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Extensions;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.LoginServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: AvaRouter.LoginView, region: AppRegions.MainRegion,typeof(ILoginServices))]
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        
    }
}