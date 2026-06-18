using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.Services.Services;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Avalonia.Collections;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ava.Xioa.Common.Themes.Services.Impl;

public partial class ViewUserInformation: ReactiveObject
{
    #region NotityProperty

    private string _account = "";

    public string Account
    {
        get => _account;
        set => this.SetProperty(ref _account, value);
    }

    private string _password = "";

    public string Password
    {
        get => _password;
        set => this.SetProperty(ref _password, value);
    }

    private string _userName = "";

    public string UserName
    {
        get => _userName;
        set => this.SetProperty(ref _userName, value);
    }

    private UserAuthEnum _userAuth = UserAuthEnum.None;

    public UserAuthEnum UserAuth
    {
        get => _userAuth;
        set => this.SetProperty(ref _userAuth, value);
    }

    #endregion
}

[PrismService(typeof(IUserUpdateDialogServices), ServiceLifetime.Singleton)]
public class UserUpdateDialogImpl : ReactiveObject, IUserUpdateDialogServices
{
    public ViewUserInformation View { get; set; } = new ViewUserInformation();
    
    public ISukiDialog? SukiDialog { get; set; }
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }
    public UserInformation? UserInformation { get; set; }

    public Func<UserInformation?, Task<bool>>? OkFuncAsync { get; set; }
    public Action? OkError { get; set; }

    private bool _isOkDialog;

    public void SetUserInformation(UserInformation userInformation)
    {
        _isOkDialog = false;
        UserInformation = userInformation;
        View.Account = userInformation.Account;
        View.UserName = userInformation.UserName;
        View.Password = userInformation.Password;
        View.UserAuth = userInformation.UserAuth;
    }

    private static string? GetCategory(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<CategoryAttribute>(false);
        if (attributes.Any())
        {
            return attributes.First().Category;
        }

        return "Properties";
    }

    private static string? GetDisplayName(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<DisplayNameAttribute>(false);
        if (attributes.Any())
        {
            return attributes.First().DisplayName;
        }

        return null;
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

        this.UserInformation.Account = View.Account;
        this.UserInformation.Password = View.Password;
        this.UserInformation.UserName = View.UserName;
        this.UserInformation.UserAuth = View.UserAuth;
        _isOkDialog = true;
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

    public void CopyUserInformationToView(UserInformation userInformation)
    {
        if (this.UserInformation is null || !_isOkDialog) return;
        userInformation.Account = this.UserInformation.Account;
        userInformation.Password = this.UserInformation.Password;
        userInformation.UserAuth = this.UserInformation.UserAuth;
        userInformation.UserName = this.UserInformation.UserName;
    }

    public UserUpdateDialogImpl(UserInformation userInformation) : this()
    {
        UserInformation = userInformation;
    }
}