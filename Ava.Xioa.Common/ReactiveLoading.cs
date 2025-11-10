using System;
using System.Threading.Tasks;
using Ava.Xioa.Common.Services;
using Avalonia.Threading;

namespace Ava.Xioa.Common;

public abstract class ReactiveLoading : ReactiveObject
{
    private bool _loading;

    public bool Loading
    {
        get => _loading;
        set => this.SetProperty(ref _loading, value);
    }


    public void LoadingInvoke(Action action)
    {
        Dispatcher.UIThread.Invoke(() => { this.Loading = true; });
        action.Invoke();
        Dispatcher.UIThread.Invoke(() => { this.Loading = false; });
    }

    public async Task LoadingInvokeAsync(Func<Task> action)
    {
        Dispatcher.UIThread.Invoke(() => { this.Loading = true; });
        await action.Invoke();
        Dispatcher.UIThread.Invoke(() => { this.Loading = false; });
    }
    
    public ReactiveLoading()
    {
    }

    public ReactiveLoading(ToastsService? toastsService) : base(toastsService)
    {
        
    }
}