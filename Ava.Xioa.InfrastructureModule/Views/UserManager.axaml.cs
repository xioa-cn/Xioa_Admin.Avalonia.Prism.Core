using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Infrastructure.Services.Services.UserServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(UserManager), region: AppRegions.HomeRegion)]
public partial class UserManager : UserControl
{
    public UserManager(IUserServices userServices)
    {
        this.DataContext = userServices;
        InitializeComponent();

        this.Loaded += (sender, args) => { userServices.Load(); };
    }

    private void DataGridLoading_Row(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.Index + 1).ToString();
    }
}