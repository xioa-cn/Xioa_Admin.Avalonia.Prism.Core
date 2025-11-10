using System;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface ICloseDialogService : IDialogBaseable, IDialogBtnCommand
{
    bool MiniSize { get; set; }

    bool Close { get; set; }

    bool Logout { get; set; }

    Action? LogoutAction { get; set; }

    Action? CloseAction { get; set; }

    Action? MiniSizeAction { get; set; }
}