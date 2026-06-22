using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Entities.SystemDbset;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Services.Services.LoginServices;
using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Ava.Xioa.Infrastructure.Services.Utils;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prism.Core.Mvvm;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Controls;
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
    private readonly OnceLoadedAsync _onLoaded;

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

        _onLoaded = new OnceLoadedAsync();

        _onLoaded.SetOnLoaded(async () =>
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
        });
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
        _mainWindowServices.IsTitleBarVisible = false;
        _mainWindowServices.ShowTitlebarBackground = false;
        _mainWindowServices.CanMove = true;
        _mainWindowServices.ShowInTaskbar = true;
        _mainWindowServices.CanFullScreen = false;
        _mainWindowServices.CanMinimize = false;
        _mainWindowServices.CanMaximize = false;
        _mainWindowServices.CanPin = false;
        _mainWindowServices.CanResize = false;
        _mainWindowServices.ShowBottomBorder = false;
        _mainWindowServices.IsMenuVisible = false;
        _mainWindowServices.TitleBarVisibilityOnFullScreen = SukiWindow.TitleBarVisibilityMode.Hidden;
        // _mainWindowServices.WindowState = WindowState.Normal;
        base.OnNavigatedTo(navigationContext);
    }

    private async Task Login()
    {
        try
        {
            LoginLoading = true;

            await Task.Delay(1000);

            var isAdmin = await IfAdminSettingApplication(this._account, this._password);

            GlobalUserInformation.Instance.Account = this._account;

            if (isAdmin)
            {
                GlobalUserInformation.Instance.UserName = "XIOA";
                GlobalUserInformation.Instance.UserAuthEnum = UserAuthEnum.Admin;
                ExecuteNavigate(
                    NavigationParametersHelper.TargetNavigationParameters(AvaRouter.HomeView,
                        AppRegions.MainRegion));
                AutoMethodVm();
                
                await _sukiDialogManager.CreateDialog()
                    .WithTitle("Application Settings")
                    .WithContent("Whether to initialize the DB related configuration")
                    .WithActionButton("Yes", async (_) =>
                        await _systemDbContext.DbFileExistOrCreateAsync(), true)
                    .WithActionButton("No", _ => { }, true)
                    .TryShowAsync();

                return;
            }

            var findUser =
                await _userInformationRepository.DbSet.FirstOrDefaultAsync(item => item.Account == this._account);
            if (findUser == null)
            {
                _sukiDialogManager.SetMessage("帳號不存在", "登录报错").OfType(NotificationType.Error).TryShow();
                return;
            }

            GlobalUserInformation.Instance.UserName = findUser.UserName;
            GlobalUserInformation.Instance.UserAuthEnum = findUser.UserAuth;

            ExecuteNavigate(
                NavigationParametersHelper.TargetNavigationParameters(AvaRouter.HomeView,
                    AppRegions.MainRegion));
            AutoMethodVm();
        }
        catch (Exception e)
        {
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

    protected override Size? AfterChangeSize { get; } = new Size(888, 550);

    [LogAspect]
    public async Task<bool> IfAdminSettingApplication(string account, string password)
    {
        if (account != "xioa" || password != "xioa") return false;

       
        return true;
    }

    public async Task LoadAsync()
    {
        await _onLoaded.LoadAsync();
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }
}
