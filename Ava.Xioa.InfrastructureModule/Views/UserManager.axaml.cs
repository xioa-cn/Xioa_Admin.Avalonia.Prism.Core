using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Services.Services.UserServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: AvaRouter.UserManager, region: AppRegions.HomeRegion,
    typeof(IUserServices))]
public partial class UserManager : UserControl
{
    public UserManager()
    {
        InitializeComponent();
    }

    private void DataGridLoading_Row(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.Index + 1).ToString();
    }
}