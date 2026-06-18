using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Input;
using Ava.Xioa.Common.Themes.Services.Services;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;

namespace Ava.Xioa.Common.Themes.Services.Impl;

[PrismService(typeof(ITextInstructionService), ServiceLifetime.Singleton)]
public class TextInstructionImpl : ITextInstructionService
{
    public ISukiDialog? SukiDialog { get; set; }
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }
    public string Title { get; set; }
    public string Message { get; set; }
    public Func<Task<bool>>? OkFuncAsync { get; set; }

    public TextInstructionImpl(string title, string message)
    {
        Title = title;
        Message = message;
        CancelCommand = new RelayCommand(Cancel);
        OkCommand = new RelayCommand(Ok);
    }

    private void Ok()
    {
        OkFuncAsync?.Invoke();
        this.CloseDialog();
    }

    private void Cancel()
    {
        this.CloseDialog();
    }
    
    public void CloseDialog()
    {
        SukiDialog?.Dismiss();
        SukiDialog?.ResetToDefault();
    }
}