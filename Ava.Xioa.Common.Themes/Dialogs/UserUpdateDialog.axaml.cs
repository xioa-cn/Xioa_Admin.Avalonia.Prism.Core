using Ava.Xioa.Common.Themes.Services.Services;
using Avalonia.Controls;

namespace Ava.Xioa.Common.Themes.Dialogs;

public partial class UserUpdateDialog : UserControl
{
    public UserUpdateDialog(IUserUpdateDialogServices userUpdateDialogServices)
    {
        this.DataContext = userUpdateDialogServices;
        InitializeComponent();
    }
}