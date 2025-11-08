using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Const;

public class GlobalUserInformation
{
    private static GlobalUserInformation? _instance;

    public static GlobalUserInformation Instance
    {
        get => _instance ??= new GlobalUserInformation();
    }

    public UserAuthEnum UserAuthEnum { get; set; } = UserAuthEnum.None;

    public bool IsLogin => this.UserAuthEnum != UserAuthEnum.None;

    public string? Account { get; set; }

    public string? UserName { get; set; }
}