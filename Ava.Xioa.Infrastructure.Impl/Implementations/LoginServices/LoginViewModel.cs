using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.LoginServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Prism.Navigation.Regions;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.LoginServices;

[PrismViewModel(typeof(ILoginServices), ServiceLifetime.Singleton)]
public partial class LoginViewModel : NavigableChangeWindowSizeViewModel, ILoginServices
{
    public ICommand LoginCommand { get; }

    private readonly IMainWindowServices _mainWindowServices;

    public LoginViewModel(IRegionManager regionManager, IMainWindowServices mainWindowServices) : base(
        regionManager, mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
        LoginCommand = new AsyncRelayCommand(Login);
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mainWindowServices.ShowInTaskbar = true;
        base.OnNavigatedTo(navigationContext);
    }

    private async Task Login()
    {
        ExecuteNavigate(
            NavigationParametersHelper.TargetNavigationParameters("HomeView",
                AppRegions.MainRegion));
    }

    protected override Size AfterChangeSize { get; } = new Size(444, 550);
}