using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Avalonia.Controls;

namespace Ava.Xioa.InfrastructureModule.Views;

[RegisterForNavigation(navigationName: nameof(HomeView), region: AppRegions.MainRegion)]
public partial class HomeView : UserControl
{
    public HomeView(IHomeServices homeServices)
    {
        this.DataContext = homeServices;
        InitializeComponent();
    }
}