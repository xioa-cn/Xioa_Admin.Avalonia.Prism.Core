using System;
using System.Threading.Tasks;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface IUserUpdateDialogServices : IDialogBaseable, IDialogBtnCommand
{
    string Account { get; set; }
    string Password { get; set; }
    string UserName { get; set; }
    UserAuthEnum UserAuth { get; set; }

    UserInformation UserInformation { get; set; }

    Func<UserInformation?, Task<bool>>? OkFuncAsync { get; set; }
    
    Action? OkError { get; set; }

    void SetUserInformation(UserInformation userInformation);
}