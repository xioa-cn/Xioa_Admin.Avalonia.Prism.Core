using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Prism;

public abstract class BindableBase : INotifyPropertyChanged, INotifyPropertyChanging
{
    private readonly object _propertyNotificationSyncRoot = new();
    private readonly object _modifiedSyncRoot = new();
    private HashSet<string?>? _deferredPropertyNameSet;
    private HashSet<string>? _modifiedProperties;
    private string[]? _modifiedPropertiesSnapshot;
    private int _deferPropertyNotificationCount;
    private bool _hasDeferredGlobalRefresh;
    private bool _isModified;
    private bool _modifiedPropertiesSnapshotDirty = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event PropertyChangingEventHandler? PropertyChanging;

    public event EventHandler<PropertyChangingCancelEventArgs>? PropertyChangingCancelable;

    public bool IsModified
    {
        get
        {
            lock (_modifiedSyncRoot)
            {
                return _isModified;
            }
        }
    }

    public IReadOnlyCollection<string> ModifiedProperties
    {
        get
        {
            lock (_modifiedSyncRoot)
            {
                if (!_modifiedPropertiesSnapshotDirty && _modifiedPropertiesSnapshot is not null)
                {
                    return _modifiedPropertiesSnapshot;
                }

                if (_modifiedProperties is null || _modifiedProperties.Count == 0)
                {
                    _modifiedPropertiesSnapshot = Array.Empty<string>();
                    _modifiedPropertiesSnapshotDirty = false;
                    return Array.Empty<string>();
                }

                var properties = new string[_modifiedProperties.Count];
                _modifiedProperties.CopyTo(properties);
                _modifiedPropertiesSnapshot = properties;
                _modifiedPropertiesSnapshotDirty = false;
                return _modifiedPropertiesSnapshot;
            }
        }
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (AreEqual(storage, value))
        {
            return false;
        }

        if (!RaisePropertyChanging(propertyName))
        {
            return false;
        }

        storage = value;
        MarkModified(propertyName);
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        if (comparer.Equals(storage, value))
        {
            return false;
        }

        if (!RaisePropertyChanging(propertyName))
        {
            return false;
        }

        storage = value;
        MarkModified(propertyName);
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
    {
        var changed = SetProperty(ref storage, value, propertyName);
        if (changed)
        {
            onChanged?.Invoke();
        }

        return changed;
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, Action<T, T> onChanged, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(onChanged);
        if (AreEqual(storage, value))
        {
            return false;
        }

        if (!RaisePropertyChanging(propertyName))
        {
            return false;
        }

        var oldValue = storage;
        storage = value;
        MarkModified(propertyName);
        RaisePropertyChanged(propertyName);
        onChanged(oldValue, value);
        return true;
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, Func<T, bool> validation, Action? onChanged = null, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation(value))
        {
            return false;
        }

        return SetProperty(ref storage, value, onChanged, propertyName);
    }

    protected virtual bool SetPropertyForce<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!RaisePropertyChanging(propertyName))
        {
            return false;
        }

        storage = value;
        MarkModified(propertyName);
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, Func<Task> onChangedAsync, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(onChangedAsync);
        var changed = SetProperty(ref storage, value, propertyName);
        if (changed)
        {
            _ = ObservePropertyChangedTaskAsync(onChangedAsync());
        }

        return changed;
    }

    protected virtual Task<bool> SetPropertyAsync<T>(ref T storage, T value, Func<Task> onChangedAsync, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(onChangedAsync);
        return SetPropertyAsyncCore(ref storage, value, _ => onChangedAsync(), CancellationToken.None, propertyName);
    }

    protected virtual Task<bool> SetPropertyAsync<T>(ref T storage, T value, Func<CancellationToken, Task> onChangedAsync, CancellationToken cancellationToken = default, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(onChangedAsync);
        return SetPropertyAsyncCore(ref storage, value, onChangedAsync, cancellationToken, propertyName);
    }

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected virtual bool RaisePropertyChanging([CallerMemberName] string? propertyName = null)
    {
        var cancelArgs = new PropertyChangingCancelEventArgs(propertyName);
        if (!OnPropertyChanging(cancelArgs))
        {
            return false;
        }

        var cancelableHandler = PropertyChangingCancelable;
        cancelableHandler?.Invoke(this, cancelArgs);
        if (cancelArgs.Cancel)
        {
            return false;
        }

        var changingHandler = PropertyChanging;
        changingHandler?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        return true;
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        if (TryDeferPropertyChanged(args.PropertyName))
        {
            return;
        }

        DispatchPropertyChanged(args);
    }

    protected virtual bool OnPropertyChanging(PropertyChangingCancelEventArgs args)
    {
        return !args.Cancel;
    }

    protected IDisposable DeferPropertyNotifications()
    {
        BeginPropertyNotifications();
        return new PropertyNotificationDeferScope(this);
    }

    protected virtual void BeginPropertyNotifications()
    {
        lock (_propertyNotificationSyncRoot)
        {
            _deferPropertyNotificationCount++;
            _deferredPropertyNameSet ??= new HashSet<string?>();
        }
    }

    protected virtual void EndPropertyNotifications()
    {
        string?[]? propertyNames = null;
        lock (_propertyNotificationSyncRoot)
        {
            if (_deferPropertyNotificationCount == 0)
            {
                return;
            }

            _deferPropertyNotificationCount--;
            if (_deferPropertyNotificationCount == 0)
            {
                propertyNames = _hasDeferredGlobalRefresh
                    ? new string?[] { string.Empty }
                    : CopyDeferredPropertyNames();
                _deferredPropertyNameSet = null;
                _hasDeferredGlobalRefresh = false;
            }
        }

        if (propertyNames is null)
        {
            return;
        }

        foreach (var propertyName in propertyNames)
        {
            DispatchPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }

    protected virtual void RaisePropertyChanged(params string?[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(propertyNames);
        foreach (var propertyName in propertyNames)
        {
            RaisePropertyChanged(propertyName);
        }
    }

    protected virtual void RaisePropertiesChanged(IEnumerable<string?> propertyNames)
    {
        ArgumentNullException.ThrowIfNull(propertyNames);
        foreach (var propertyName in propertyNames)
        {
            RaisePropertyChanged(propertyName);
        }
    }

    protected virtual bool SetProperties(Action updateFields, params string?[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(updateFields);
        updateFields();
        RaisePropertyChanged(propertyNames);
        return propertyNames.Length > 0;
    }

    protected virtual bool SetPropertiesDeferred(Action updateFields, params string?[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(updateFields);
        using (DeferPropertyNotifications())
        {
            updateFields();
            RaisePropertyChanged(propertyNames);
        }

        return propertyNames.Length > 0;
    }

    protected virtual bool SetProperties(Func<bool> updateFields, params string?[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(updateFields);
        var changed = updateFields();
        if (changed)
        {
            RaisePropertyChanged(propertyNames);
        }

        return changed;
    }

    protected virtual bool SetPropertiesDeferred(Func<bool> updateFields, params string?[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(updateFields);
        using (DeferPropertyNotifications())
        {
            var changed = updateFields();
            if (changed)
            {
                RaisePropertyChanged(propertyNames);
            }

            return changed;
        }
    }

    protected virtual bool SetPropertySilently<T>(ref T storage, T value)
    {
        if (AreEqual(storage, value))
        {
            return false;
        }

        storage = value;
        return true;
    }

    protected virtual void AcceptChanges()
    {
        lock (_modifiedSyncRoot)
        {
            _modifiedProperties?.Clear();
            _isModified = false;
            _modifiedPropertiesSnapshot = Array.Empty<string>();
            _modifiedPropertiesSnapshotDirty = false;
        }

        RaisePropertyChanged(nameof(IsModified), nameof(ModifiedProperties));
    }

    protected virtual void MarkModified([CallerMemberName] string? propertyName = null)
    {
        lock (_modifiedSyncRoot)
        {
            _isModified = true;
            if (!string.IsNullOrEmpty(propertyName))
            {
                _modifiedProperties ??= new HashSet<string>(StringComparer.Ordinal);
                if (_modifiedProperties.Add(propertyName))
                {
                    _modifiedPropertiesSnapshotDirty = true;
                    _modifiedPropertiesSnapshot = null;
                }
            }
        }

        RaisePropertyChanged(nameof(IsModified), nameof(ModifiedProperties));
    }

    protected virtual void OnAsyncPropertyChangedCallbackException(Exception exception)
    {
        BindableBaseOptions.AsyncPropertyChangedExceptionHandler?.Invoke(exception);
    }

    private async Task ObservePropertyChangedTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnAsyncPropertyChangedCallbackException(ex);
        }
    }

    private Task<bool> SetPropertyAsyncCore<T>(
        ref T storage,
        T value,
        Func<CancellationToken, Task> onChangedAsync,
        CancellationToken cancellationToken,
        string? propertyName)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var changed = SetProperty(ref storage, value, propertyName);
        if (!changed)
        {
            return Task.FromResult(false);
        }

        return CompletePropertyChangedAsync(onChangedAsync, cancellationToken);
    }

    private async Task<bool> CompletePropertyChangedAsync(Func<CancellationToken, Task> onChangedAsync, CancellationToken cancellationToken)
    {
        try
        {
            await onChangedAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            OnAsyncPropertyChangedCallbackException(ex);
            throw;
        }
    }

    private bool TryDeferPropertyChanged(string? propertyName)
    {
        lock (_propertyNotificationSyncRoot)
        {
            if (_deferPropertyNotificationCount == 0)
            {
                return false;
            }

            _deferredPropertyNameSet ??= new HashSet<string?>();
            if (string.IsNullOrEmpty(propertyName))
            {
                if (!_hasDeferredGlobalRefresh)
                {
                    _hasDeferredGlobalRefresh = true;
                    _deferredPropertyNameSet.Clear();
                    _deferredPropertyNameSet.Add(string.Empty);
                }

                return true;
            }

            if (_hasDeferredGlobalRefresh)
            {
                return true;
            }

            _deferredPropertyNameSet.Add(propertyName);
            return true;
        }
    }

    private string?[]? CopyDeferredPropertyNames()
    {
        var deferredPropertyNameSet = _deferredPropertyNameSet;
        var count = deferredPropertyNameSet?.Count ?? 0;
        if (count == 0)
        {
            return null;
        }

        var propertyNames = new string?[count];
        deferredPropertyNameSet!.CopyTo(propertyNames);
        return propertyNames;
    }

    private static bool AreEqual<T>(T left, T right)
    {
        return BindableBaseOptions.DefaultValueComparer is { } comparer
            ? comparer.Equals(left, right)
            : EqualityComparer<T>.Default.Equals(left, right);
    }

    private void DispatchPropertyChanged(PropertyChangedEventArgs args)
    {
        var handler = PropertyChanged;
        if (handler is null)
        {
            return;
        }

        if (BindableBaseOptions.DispatchPropertyNotifications)
        {
            BindableBaseOptions.Validate();
            var dispatcher = BindableBaseOptions.PropertyNotificationDispatcher!;
            dispatcher(() => handler(this, args));
            return;
        }

        handler(this, args);
    }

    private sealed class PropertyNotificationDeferScope : IDisposable
    {
        private readonly BindableBase _owner;
        private int _disposed;

        public PropertyNotificationDeferScope(BindableBase owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _owner.EndPropertyNotifications();
            }
        }
    }
}