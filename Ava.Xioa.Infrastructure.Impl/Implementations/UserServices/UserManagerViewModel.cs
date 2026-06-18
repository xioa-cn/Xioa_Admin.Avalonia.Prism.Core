using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Themes.Dialogs;
using Ava.Xioa.Common.Themes.I18n;
using Ava.Xioa.Common.Themes.Services.Impl;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Common.Themes.Utils;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Connectlayer.Dialogs;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Services.Services.UserServices;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.UserServices;

[PrismViewModel(typeof(IUserServices), ServiceLifetime.Singleton)]
public partial class UserManagerViewModel : ReactiveLoading, IUserServices, IAvaloniaI18Nable
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

    public UserManagerViewModel(IUserInformationRepository userInformationRepository, IToastsService toastsService,
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
        if (arg is null)
        {
            ToastsService?.ShowError(this.Tr("delete", "删除"),
                this.Tr("selectInfoIsNull", "选择得信息为空"), 2000);
            return;
        }

        ITextInstructionService textInstructionService = new TextInstructionImpl(this.Tr("deleteOperate", "是否进行删除操作"),
            this.Tr("askDeleteOperateMessage", "注:删除操作无法撤回"));

        textInstructionService.OkFuncAsync = async () =>
        {
            var deleteResult = await _userInformationRepository.DbSet.Where(item => item.Id == arg.Id)
                .ExecuteDeleteAsync();

            if (deleteResult == 1)
            {
                ToastsService?.ShowSuccess(this.Tr("delete", "删除"),
                    this.Tr("deleteUserSuccess", "删除用户成功"), 2000);
                UserInformation.Remove(arg);
                return true;
            }

            ToastsService?.ShowSuccess(this.Tr("delete", "删除"),
                this.Tr("deleteUserError", "删除用户失败"), 2000);
            return false;
        };

        var dialog = _sukiDialogManager.CreateVmDialog(textInstructionService);

        TextInstructionDialog textInstructionDialog = new TextInstructionDialog(textInstructionService);

        await dialog.WithTitle(this.Tr("deleteUserInformation", "删除用户信息"))
            .WithContent(textInstructionDialog).OfType(NotificationType.Error)
            .Dismiss().ByClickingBackground().WithAsync().TryShowAsync();
    }

    private async Task UpdateFunc(UserInformation? arg)
    {
        if (arg is null)
        {
            ToastsService?.ShowError(this.Tr("amend", "修改"),
                this.Tr("selectInfoIsNull", "选择得信息为空"), 2000);
            return;
        }

        var dialog = _sukiDialogManager.CreateVmDialog(_userUpdateDialogServices);
        _userUpdateDialogServices.OkFuncAsync ??= async information =>
        {
            if (information is null)
            {
                return false;
            }

            await this.LoadingInvokeAsync(async () =>
            {
                var result = await _userInformationRepository.DbSet.Where(item => item.Id == information.Id)
                    .ExecuteUpdateAsync(item => item.SetProperty(
                            x => x.Account, information.Account)
                        .SetProperty(x => x.Password, information.Password)
                        .SetProperty(x => x.UserName, information.UserName)
                        .SetProperty(x => x.UserAuth, information.UserAuth));
            });

            return true;
        };


        _userUpdateDialogServices.SetUserInformation(
            DeepCopyHelper.JsonClone(arg) ?? throw new SerializationException());
        _userUpdateDialog ??= new UserUpdateDialog(_userUpdateDialogServices);
        var dialogResult = await dialog.WithTitle(this.Tr("updateUserInformation", "修改用户信息"))
            .WithContent(_userUpdateDialog).OfType(NotificationType.Information)
            .Dismiss().ByClickingBackground().WithAsync().TryShowAsync();

        _userUpdateDialogServices.CopyUserInformationToView(arg);

        await Task.Delay(500);
    }

    private async Task AddUserFunc()
    {
        // 添加用户 Dialog
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

        ToastsService?.ShowSuccess(this.Tr("search", "查询"),
            this.Tr("searchUserSuccess", "查询用户成功"), 2000);
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