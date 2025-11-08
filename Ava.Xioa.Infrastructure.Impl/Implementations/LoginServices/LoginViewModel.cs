using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Entities.SystemDbset;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Services.Services.LoginServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Avalonia.Controls.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Navigation.Regions;
using SukiUI.Dialogs;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.LoginServices;

[PrismViewModel(typeof(ILoginServices), ServiceLifetime.Singleton)]
public partial class LoginViewModel : NavigableChangeWindowSizeViewModel, ILoginServices
{
    [ObservableBindProperty] private string _account = string.Empty;
    [ObservableBindProperty] private string _password = string.Empty;
    [ObservableBindProperty] private bool _isRemember;
    [ObservableBindProperty] private bool _isAutoLogin;
    [ObservableBindProperty] private bool _loginLoading;
    private readonly SystemDbContext _systemDbContext;
    public ICommand LoginCommand { get; }
    public ICommand ExitCommand { get; }

    private readonly IMainWindowServices _mainWindowServices;
    private readonly IUserInformationRepository _userInformationRepository;
    private readonly INowUserInformationRepository _nowUserInformationRepository;

    private readonly ISukiDialogManager _sukiDialogManager;

    public LoginViewModel(IEventAggregator eventAggregator, IRegionManager regionManager,
        IMainWindowServices mainWindowServices,
        SystemDbContext systemDbContext, IUserInformationRepository userInformationRepository,
        ISukiDialogManager sukiDialogManager, INowUserInformationRepository nowUserInformationRepository) : base(
        eventAggregator, regionManager, mainWindowServices)
    {
        _mainWindowServices = mainWindowServices;
        _systemDbContext = systemDbContext;
        _userInformationRepository = userInformationRepository;
        _sukiDialogManager = sukiDialogManager;
        _nowUserInformationRepository = nowUserInformationRepository;
        LoginCommand = new AsyncRelayCommand(Login);

        ExitCommand = new RelayCommand(Exit);
    }

    private void Exit()
    {
        EventAggregator?.GetEvent<ExitApplicationEvent>().Publish(new TokenKeyPubSubEvent<Exit>("ExitApplication",
            new Exit()
            {
                ExitCode = 0
            }));
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        _mainWindowServices.IsTitleBarVisible = true;
        _mainWindowServices.ShowTitlebarBackground = true;
        _mainWindowServices.CanMove = true;
        _mainWindowServices.ShowInTaskbar = true;
        base.OnNavigatedTo(navigationContext);
    }

    private async Task Login()
    {
        try
        {
            LoginLoading = true;

            await Task.Delay(1000);

            var isAdmin = await IfAdminSettingApplication(this._account, this._password);
            if (isAdmin)
            {
                ExecuteNavigate(
                    NavigationParametersHelper.TargetNavigationParameters("HomeView",
                        AppRegions.MainRegion));
                AutoMethodVm();
                return;
            }

            var findUser = _userInformationRepository.DbSet.FirstOrDefault(item => item.Account == this._account);
            if (findUser == null)
            {
                _sukiDialogManager.SetMessage("帳號不存在", "登录报错").OfType(NotificationType.Error).TryShow();
                return;
            }

            ExecuteNavigate(
                NavigationParametersHelper.TargetNavigationParameters("HomeView",
                    AppRegions.MainRegion));

            AutoMethodVm();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            LoginLoading = false;
        }
    }

    private void AutoMethodVm()
    {
        var find = _nowUserInformationRepository.DbSet.FirstOrDefault();

        if (find is not null)
        {
            find.Account = this._account;
            find.RememberPassword = this._isRemember;
            find.AutoLogin = this._isAutoLogin;
            if (this._isRemember)
                find.Password = this._password;
        }
        else
        {
            _nowUserInformationRepository.DbSet.Add(new NowUserInformation
            {
                Account = this._account,
                Password = this._password,
                RememberPassword = this._isRemember,
                AutoLogin = this._isAutoLogin
            });
        }

        _nowUserInformationRepository.DbContext.SaveChanges();
    }

    protected override Size AfterChangeSize { get; } = new Size(888, 550);

    public async void Loaded()
    {
        if (!_nowUserInformationRepository.DbIsExist)
        {
            return;
        }

        var nowUser = _nowUserInformationRepository.DbSet.FirstOrDefault();

        if (nowUser is null) return;

        this.Account = nowUser.Account;

        if (!nowUser.RememberPassword) return;
        this.Password = nowUser.Password;
        this.IsRemember = nowUser.RememberPassword;

        if (!nowUser.AutoLogin) return;
        this.IsAutoLogin = nowUser.AutoLogin;

        await Login();
    }

    private async Task<bool> IfAdminSettingApplication(string account, string password)
    {
        if (account != "xioa" || password != "xioa") return false;

        _sukiDialogManager.CreateDialog()
            .WithTitle("Application Settings")
            .WithContent("Whether to initialize the DB related configuration")
            .WithActionButton("Yes", async (_) =>
                await _systemDbContext.DbFileExistOrCreateAsync(), true)
            .WithActionButton("No", _ => { }, true)
            .TryShow();

        return true;
    }
}