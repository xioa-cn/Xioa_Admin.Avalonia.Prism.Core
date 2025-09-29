using System.Windows.Input;

namespace Ava.Xioa.Infrastructure.Services.Services.LoginServices;

public interface ILoginServices
{
    ICommand LoginCommand { get; }
}