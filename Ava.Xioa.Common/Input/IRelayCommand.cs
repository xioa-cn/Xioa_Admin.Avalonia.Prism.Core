﻿using System.Windows.Input;

namespace Ava.Xioa.Common.Input;

public interface IRelayCommand : ICommand
{
    void NotifyCanExecuteChanged();
}