using System;
using System.Threading.Tasks;
using Ava.Xioa.Common.Themes.Services.Impl;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Avalonia.Collections;
using SukiUI.Controls;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface IUserUpdateDialogServices : IDialogBaseable, IDialogBtnCommand
{
    ViewUserInformation View { get; set; }
    UserInformation UserInformation { get; set; }
    Func<UserInformation?, Task<bool>>? OkFuncAsync { get; set; }
    Action? OkError { get; set; }
    void SetUserInformation(UserInformation userInformation);

    void CopyUserInformationToView(UserInformation userInformation);
}