namespace Prism;

public static class BindableBaseOptions
{
    private static bool _dispatchPropertyNotifications;
    private static Action<Action>? _propertyNotificationDispatcher;

    public static bool DispatchPropertyNotifications
    {
        get => _dispatchPropertyNotifications;
        set
        {
            if (value && _propertyNotificationDispatcher is null)
            {
                throw new InvalidOperationException("PropertyNotificationDispatcher must be configured before enabling DispatchPropertyNotifications.");
            }

            _dispatchPropertyNotifications = value;
        }
    }

    public static Action<Action>? PropertyNotificationDispatcher
    {
        get => _propertyNotificationDispatcher;
        set
        {
            if (value is null && _dispatchPropertyNotifications)
            {
                throw new InvalidOperationException("PropertyNotificationDispatcher cannot be cleared while DispatchPropertyNotifications is enabled.");
            }

            _propertyNotificationDispatcher = value;
        }
    }

    public static IEqualityComparer<object?>? DefaultValueComparer { get; set; }

    public static Action<Exception>? AsyncPropertyChangedExceptionHandler { get; set; }

    public static void Validate()
    {
        if (DispatchPropertyNotifications && PropertyNotificationDispatcher is null)
        {
            throw new InvalidOperationException("PropertyNotificationDispatcher must be configured when DispatchPropertyNotifications is enabled.");
        }
    }
}