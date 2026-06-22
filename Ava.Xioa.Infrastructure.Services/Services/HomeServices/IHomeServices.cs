using Ava.Xioa.Infrastructure.Services.Services.WindowServices;
using Prism.Core.Mvvm;

namespace Ava.Xioa.Infrastructure.Services.Services.HomeServices;

public interface IHomeServices : IVmLoaded
{
    IMainWindowServices MainWindowServices { get; }

    INavigableMenuServices NavigableMenuServices { get; }
}