using System;
using System.Threading.Tasks;

namespace Ava.Xioa.Infrastructure.Services.Services.Services;

public interface ILoadingable
{
    bool Loading { get; set; }
    
    void LoadingInvoke(Action action);

    Task LoadingInvokeAsync(Func<Task> action);
}