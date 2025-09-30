using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(UserManager), region: AppRegions.HomeRegion)]
public partial class UserManager : UserControl
{
    public UserManager()
    {
        InitializeComponent();
    }
}