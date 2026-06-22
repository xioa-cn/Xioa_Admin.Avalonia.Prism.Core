using System.Windows.Input;
using Prism.Core.Mvvm;

namespace Ava.Xioa.Infrastructure.Services.Services.LoginServices;

public interface ILoginServices : IVmLoadedAsync
{
    string Account { get; set; }

    string Password { get; set; }

    bool IsRemember { get; set; }

    bool IsAutoLogin { get; set; }

    bool LoginLoading { get; set; }
    
    ICommand LoginCommand { get; }

    ICommand ExitCommand { get; }
}