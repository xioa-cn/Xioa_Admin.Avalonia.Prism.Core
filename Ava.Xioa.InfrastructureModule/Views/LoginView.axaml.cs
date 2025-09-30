using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Infrastructure.Services.Services.LoginServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(LoginView), region: AppRegions.MainRegion)]
public partial class LoginView : UserControl
{
    public LoginView(ILoginServices loginServices)
    {
        this.DataContext = loginServices;
        InitializeComponent();
    }
}