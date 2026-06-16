using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Const;

public partial class GlobalUserInformation : ObservableBindBase
{
    private static GlobalUserInformation? _instance;

    public static GlobalUserInformation Instance
    {
        get => _instance ??= new GlobalUserInformation();
    }

    private UserAuthEnum _userAuthEnum = UserAuthEnum.None;

    public UserAuthEnum UserAuthEnum
    {
        get => _userAuthEnum;
        set => SetProperty(ref _userAuthEnum, value);
    }

    public bool IsLogin => this.UserAuthEnum != UserAuthEnum.None;

    private string? _account = string.Empty;

    public string? Account
    {
        get => _account;
        set => SetProperty(ref _account, value);
    }

    private string? _userName = string.Empty;

    public string? UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }
}