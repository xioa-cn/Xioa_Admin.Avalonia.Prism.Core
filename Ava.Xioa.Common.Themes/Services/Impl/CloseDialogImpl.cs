using System;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Themes.Services.Services;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Impl;

[PrismService(typeof(ICloseDialogService), ServiceLifetime.Singleton)]
public partial class CloseDialogImpl : ReactiveObject, ICloseDialogService
{
    private bool _close;

    public bool Close
    {
        get => _close;
        set => this.SetProperty(ref _close, value);
    }

    private bool _miniSize = true;

    public bool MiniSize
    {
        get => _miniSize;
        set => this.SetProperty(ref _miniSize, value);
    }

    private bool _logout;

    public bool Logout
    {
        get => _logout;
        set => this.SetProperty(ref _logout, value);
    }

    public Action? LogoutAction { get; set; }
    public Action? CloseAction { get; set; }
    public Action? MiniSizeAction { get; set; }

    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }

    private ISukiDialog? _sukiDialog;

    public CloseDialogImpl()
    {
        CancelCommand = new RelayCommand(Cancel);
        OkCommand = new RelayCommand(Ok);
    }

    private void Ok()
    {
        if (this._close)
        {
            CloseAction?.Invoke();
        }

        if (this._miniSize)
        {
            MiniSizeAction?.Invoke();
        }

        if (this._logout)
        {
            LogoutAction?.Invoke();
        }

        CloseDialog();
    }

    public void SetDialog(ISukiDialog sukiDialog)
    {
        _sukiDialog = sukiDialog;
    }

    private void Cancel()
    {
        CloseDialog();
    }

    private void CloseDialog()
    {
        _sukiDialog?.Dismiss();
        _sukiDialog?.ResetToDefault();
    }
}