using Ava.Xioa.Common.Themes.Services.Services;
using Avalonia.Controls;

namespace Ava.Xioa.Common.Themes.Dialogs;

public partial class CloseDialog : UserControl
{
    public CloseDialog(ICloseDialogService closeDialogService)
    {
        this.DataContext = closeDialogService;
        InitializeComponent();
    }
}