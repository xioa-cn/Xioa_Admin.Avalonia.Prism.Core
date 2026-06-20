using System;
using System.Threading.Tasks;

namespace Prism.Core.Mvvm;

public interface IVmLoaded
{
    void Load();

    void Unload();
}

public interface IVmLoadedAsync
{
    Task LoadAsync();

    Task UnloadAsync();
}

public class OnceLoaded
{
    private Action? _onLoaded;
    private Action? _onUnloaded;

    public void SetOnLoaded(Action onLoaded)
    {
        ArgumentNullException.ThrowIfNull(onLoaded);

        void Once()
        {
            _onLoaded -= Once;
            onLoaded();
        }

        _onLoaded += Once;
    }

    public void SetOnUnLoaded(Action onUnloaded)
    {
        ArgumentNullException.ThrowIfNull(onUnloaded);

        void Once()
        {
            _onUnloaded -= Once;
            onUnloaded();
        }

        _onUnloaded += Once;
    }

    public void Load()
    {
        _onLoaded?.Invoke();
    }

    public void Unload()
    {
        _onUnloaded?.Invoke();
    }
}

public class OnceLoadedAsync
{
    private Func<Task>? _onLoadedAsync;
    private Func<Task>? _onUnloadedAsync;

    public void SetOnLoaded(Func<Task> onLoadedAsync)
    {
        ArgumentNullException.ThrowIfNull(onLoadedAsync);

        async Task OnceAsync()
        {
            _onLoadedAsync -= OnceAsync;
            await onLoadedAsync().ConfigureAwait(false);
        }

        _onLoadedAsync += OnceAsync;
    }

    public void SetOnUnLoaded(Func<Task> onUnloadedAsync)
    {
        ArgumentNullException.ThrowIfNull(onUnloadedAsync);

        async Task OnceAsync()
        {
            _onUnloadedAsync -= OnceAsync;
            await onUnloadedAsync().ConfigureAwait(false);
        }

        _onUnloadedAsync += OnceAsync;
    }

    public Task LoadAsync()
    {
        return _onLoadedAsync?.Invoke() ?? Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        return _onUnloadedAsync?.Invoke() ?? Task.CompletedTask;
    }
}