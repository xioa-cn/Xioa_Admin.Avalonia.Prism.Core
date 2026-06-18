using System;
using System.Threading.Tasks;

namespace Ava.Xioa.Common.Themes.Services.Services;

public interface ITextInstructionService : IDialogBaseable, IDialogBtnCommand
{
    string Title { get; set; }
    string Message { get; set; }
    
    Func<Task<bool>>? OkFuncAsync { get; set; }
}