using Ava.Xioa.Infrastructure.Services.Services.WindowServices;

namespace Ava.Xioa.Infrastructure.Services.Services.HomeServices;

public interface IHomeServices
{
    IMainWindowServices MainWindowServices { get; }

    INavigableMenuServices NavigableMenuServices { get; }
}