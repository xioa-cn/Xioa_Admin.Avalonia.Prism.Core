using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Themes.Dialogs;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Common.Themes.Utils;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Services.Services.UserServices;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.UserServices;

[PrismViewModel(typeof(IUserServices), ServiceLifetime.Singleton)]
public partial class UserManagerViewModel : ReactiveLoading, IUserServices
{
    [ObservableBindProperty] private bool _isDataGridColumnsResizable;
    [ObservableBindProperty] private string _searchInformation = string.Empty;

    private UserUpdateDialog? _userUpdateDialog;

    private readonly IUserUpdateDialogServices _userUpdateDialogServices;

    public ObservableCollection<UserInformation> UserInformation { get; }

    public ICommand DeleteUserInformationCommand { get; }
    public ICommand AddUserInformationCommand { get; }
    public ICommand SearchUserInformationCommand { get; }
    public ICommand UpdateUserInformationCommand { get; }
    public ICommand SaveUserInformationCommand { get; }

    private readonly ISukiDialogManager _sukiDialogManager;

    private readonly IUserInformationRepository _userInformationRepository;

    public UserManagerViewModel(IUserInformationRepository userInformationRepository, ToastsService toastsService,
        IUserUpdateDialogServices userUpdateDialogServices, ISukiDialogManager sukiDialogManager) :
        base(toastsService)
    {
        _userInformationRepository = userInformationRepository;
        _userUpdateDialogServices = userUpdateDialogServices;
        _sukiDialogManager = sukiDialogManager;

        UserInformation = new ObservableCollection<UserInformation>();

        DeleteUserInformationCommand = new AsyncRelayCommand<UserInformation>(DeleteUserFunc);
        UpdateUserInformationCommand = new AsyncRelayCommand<UserInformation>(UpdateFunc);
        AddUserInformationCommand = new AsyncRelayCommand(AddUserFunc);
        SearchUserInformationCommand = new AsyncRelayCommand(SearchFunc);
        SaveUserInformationCommand = new AsyncRelayCommand(SaveFunc);
    }

    #region Command

    private async Task DeleteUserFunc(UserInformation? arg)
    {
        ToastsService?.ShowSuccess("删除", "删除用户成功", 2000);
    }

    private SukiDialogBuilder? dialog;

    private async Task UpdateFunc(UserInformation? arg)
    {
        dialog ??= _sukiDialogManager.CreateVmDialog(_userUpdateDialogServices);
        _userUpdateDialog ??= new UserUpdateDialog(_userUpdateDialogServices);
        await dialog.WithTitle("修改用户信息")
            .WithContent(_userUpdateDialog).OfType(NotificationType.Information)
            .Dismiss().ByClickingBackground().WithAsync().TryShowAsync();
        await Task.Delay(500);
    }

    private async Task AddUserFunc()
    {
    }

    private Task SaveFunc()
    {
        throw new System.NotImplementedException();
    }

    private async Task SearchFunc()
    {
        await this.LoadingInvokeAsync(async () =>
        {
            var users = _userInformationRepository.DbSet.Where(item => item.UserName.Contains(this._searchInformation)
                                                                       || item.Account.Contains(
                                                                           this._searchInformation)).ToArray();
            await Task.Delay(1000);
            Dispatcher.UIThread.Invoke(() =>
            {
                UserInformation.Clear();
                UserInformation.AddRange(users);
            });
        });

        ToastsService?.ShowSuccess("查询", "查询用户成功", 2000);
    }

    #endregion


    private async Task InitializedUserInformation()
    {
        var users = _userInformationRepository.DbSet.ToArray();
        await Task.Delay(1000);
        Dispatcher.UIThread.Invoke(() =>
        {
            UserInformation.Clear();
            UserInformation.AddRange(users);
        });
    }

    public void Load()
    {
        Task.Factory.StartNew(() => this.LoadingInvokeAsync(InitializedUserInformation));
    }
}