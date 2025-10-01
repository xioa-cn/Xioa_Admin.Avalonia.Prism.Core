using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Entities.Models;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;

namespace Ava.Xioa.Connectlayer.Global;

public partial class GlobalLoginInformation : ReactiveObject
{
    private static GlobalLoginInformation? _globalLoginInformation;

    public static GlobalLoginInformation Instance
    {
        get
        {
            if (_globalLoginInformation == null)
                _globalLoginInformation = new GlobalLoginInformation();
            return _globalLoginInformation;
        }
    }

    private GlobalLoginInformation()
    {
        UserName = "未登录";
        Account = "未登录";
        UserAuth = UserAuthEnum.Employee;
    }

    [ObservableBindProperty] private bool _LoginStatus;
    [ObservableBindProperty] private string _UserName;
    [ObservableBindProperty] private string _Account;
    [ObservableBindProperty] private UserAuthEnum _UserAuth;

    private UserInformation? _LoginUserInformation;

    public void Login(UserInformation userInformation)
    {
        LoginStatus = true;
        _LoginUserInformation = userInformation;

        this.Account = userInformation.Account;
        this.UserName = userInformation.UserName;
        this.UserAuth = userInformation.UserAuth;
    }

    public void ChangeUserInformation(UserInformation userInformation)
    {
        _LoginUserInformation = userInformation;

        this.Account = userInformation.Account;
        this.UserName = userInformation.UserName;
        this.UserAuth = userInformation.UserAuth;
    }

    public void Logout()
    {
        LoginStatus = false;
        _LoginUserInformation = null;
    }
}