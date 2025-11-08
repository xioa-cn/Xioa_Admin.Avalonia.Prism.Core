using System;
using System.Windows.Input;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface ICloseDialogService
{
    void SetDialog(ISukiDialog sukiDialog);
    ICommand CancelCommand { get; }
    ICommand OkCommand { get; }
    
    bool MiniSize { get; set; }
    
    bool Close { get; set; }
    
    bool Logout { get; set; }
    
    Action? LogoutAction { get; set; }
    
    Action? CloseAction { get; set; }
    
    Action? MiniSizeAction { get; set; }
}