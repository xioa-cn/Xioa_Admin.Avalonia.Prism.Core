using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Impl;

[PrismService(typeof(IUserUpdateDialogServices), ServiceLifetime.Singleton)]
public class UserUpdateDialogImpl : ReactiveObject, IUserUpdateDialogServices
{
    #region NotityProperty

    private string _account;

    public string Account
    {
        get => _account;
        set => this.SetProperty(ref _account, value);
    }

    private string _password;

    public string Password
    {
        get => _password;
        set => this.SetProperty(ref _password, value);
    }

    private string _userName;

    public string UserName
    {
        get => _userName;
        set => this.SetProperty(ref _userName, value);
    }

    private UserAuthEnum _userAuth;

    public UserAuthEnum UserAuth
    {
        get => _userAuth;
        set => this.SetProperty(ref _userAuth, value);
    }

    #endregion


    public ISukiDialog? SukiDialog { get; set; }
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }
    public UserInformation? UserInformation { get; set; }

    public Func<UserInformation?, Task<bool>>? OkFuncAsync { get; set; }
    public Action? OkError { get; set; }

    public void SetUserInformation(UserInformation userInformation)
    {
        UserInformation = userInformation;
    }

    public UserUpdateDialogImpl()
    {
        CancelCommand = new RelayCommand(Cancel);
        OkCommand = new RelayCommand(Ok);
    }

    private async void Ok()
    {
        if (this.UserInformation is null)
        {
            throw new NullReferenceException(nameof(UserInformation));
        }

        this.UserInformation.Account = Account;
        this.UserInformation.Password = Password;
        this.UserInformation.UserName = UserName;
        this.UserInformation.UserAuth = UserAuth;
        if (OkFuncAsync is null)
        {
            CloseDialog();
            return;
        }

        var result = await OkFuncAsync.Invoke(this.UserInformation);
        if (result)
        {
            CloseDialog();
        }
        else
        {
            OkError?.Invoke();
        }
    }

    public void CloseDialog()
    {
        SukiDialog?.Dismiss();
        SukiDialog?.ResetToDefault();
    }

    private void Cancel()
    {
        CloseDialog();
    }

    public UserUpdateDialogImpl(UserInformation userInformation) : this()
    {
        UserInformation = userInformation;
    }
}