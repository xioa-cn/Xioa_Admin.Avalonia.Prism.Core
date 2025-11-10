using System.Collections.ObjectModel;
using System.Windows.Input;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Ava.Xioa.Infrastructure.Services.Services.Services;

namespace Ava.Xioa.Infrastructure.Services.Services.UserServices;

public interface IUserServices : ILoadingable, IVmLoadable
{
    ObservableCollection<UserInformation> UserInformation { get; }

    string SearchInformation { get; set; }
    bool IsDataGridColumnsResizable { get; set; }

    ICommand DeleteUserInformationCommand { get; }
    ICommand AddUserInformationCommand { get; }
    ICommand SearchUserInformationCommand { get; }
    ICommand UpdateUserInformationCommand { get; }
    ICommand SaveUserInformationCommand { get; }
}