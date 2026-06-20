using System;

namespace Prism.Ioc;

public static class IContainerRegistryExtensions
{
    public static bool Unregister(this IContainerRegistry registry, Type type, string? name = null)
    {
        return registry switch
        {
            IDynamicContainerPartition dynamicPartition => dynamicPartition.Unregister(type, name),
            _ => false
        };
    }

    public static IContainerRegistry Register<TFrom, TTo>(this IContainerRegistry registry)
        where TTo : TFrom
    {
        return registry.Register(typeof(TFrom), typeof(TTo));
    }

    public static IContainerRegistry Register<TFrom, TTo>(this IContainerRegistry registry, string name)
        where TTo : TFrom
    {
        return registry.Register(typeof(TFrom), typeof(TTo), name);
    }

    public static IContainerRegistry Register<T>(this IContainerRegistry registry)
    {
        return registry.Register(typeof(T));
    }

    public static IContainerRegistry Register<T>(this IContainerRegistry registry, string name)
    {
        return registry.Register(typeof(T), name);
    }

    public static IContainerRegistry RegisterSingleton<TFrom, TTo>(this IContainerRegistry registry)
        where TTo : TFrom
    {
        return registry.RegisterSingleton(typeof(TFrom), typeof(TTo));
    }

    public static IContainerRegistry RegisterSingleton<TFrom, TTo>(this IContainerRegistry registry, string name)
        where TTo : TFrom
    {
        return registry.RegisterSingleton(typeof(TFrom), typeof(TTo), name);
    }

    public static IContainerRegistry RegisterSingleton<T>(this IContainerRegistry registry)
    {
        return registry.RegisterSingleton(typeof(T));
    }

    public static IContainerRegistry RegisterInstance<T>(this IContainerRegistry registry, T instance)
        where T : notnull
    {
        return registry.RegisterInstance(typeof(T), instance);
    }

    public static IContainerRegistry RegisterInstance<T>(this IContainerRegistry registry, T instance, string name)
        where T : notnull
    {
        return registry.RegisterInstance(typeof(T), instance, name);
    }

    public static IContainerRegistry Register<T>(this IContainerRegistry registry, Func<IContainerProvider, T> factory)
        where T : notnull
    {
        return registry.RegisterFactory(typeof(T), provider => factory(provider));
    }

    public static IContainerRegistry Register<T>(this IContainerRegistry registry, Func<IContainerProvider, T> factory,
        string name)
        where T : notnull
    {
        return registry.RegisterFactory(typeof(T), provider => factory(provider), name);
    }

    public static IContainerRegistry RegisterSingleton<T>(this IContainerRegistry registry,
        Func<IContainerProvider, T> factory)
        where T : notnull
    {
        return registry.RegisterSingletonFactory(typeof(T), provider => factory(provider));
    }

    public static IContainerRegistry RegisterSingleton<T>(this IContainerRegistry registry,
        Func<IContainerProvider, T> factory, string name)
        where T : notnull
    {
        return registry.RegisterSingletonFactory(typeof(T), provider => factory(provider), name);
    }

    public static bool IsRegistered<T>(this IContainerProvider containerProvider)
    {
        return containerProvider.IsRegistered(typeof(T));
    }

    public static bool IsRegistered<T>(this IContainerProvider containerProvider, string name)
    {
        return containerProvider.IsRegistered(typeof(T), name);
    }

    public static IContainerRegistry RegisterDialog<TView>(this IContainerRegistry registry, string? name = null)
    {
        return registry.RegisterForNavigation<TView>(name);
    }

    public static IContainerRegistry RegisterDialog<TView, TViewModel>(this IContainerRegistry registry,
        string? name = null)
        where TViewModel : Prism.Dialogs.IDialogAware
    {
        return registry.RegisterForNavigation<TView, TViewModel>(name);
    }

    public static IContainerRegistry RegisterDialogWindow<TWindow>(this IContainerRegistry registry)
        where TWindow : Prism.Dialogs.IDialogWindow
    {
        return registry.Register(typeof(Prism.Dialogs.IDialogWindow), typeof(TWindow));
    }

    public static IContainerRegistry RegisterDialogWindow<TWindow>(this IContainerRegistry registry, string name)
        where TWindow : Prism.Dialogs.IDialogWindow
    {
        return registry.Register(typeof(Prism.Dialogs.IDialogWindow), typeof(TWindow), name);
    }

    public static IContainerRegistry RegisterForNavigation(this IContainerRegistry registry, Type type, string name)
    {
        registry.Register(type);
        return registry.Register(typeof(object), type, name);
    }

    public static IContainerRegistry RegisterForNavigation<TView>(this IContainerRegistry registry, string? name = null)
    {
        var type = typeof(TView);
        var registrationName = string.IsNullOrWhiteSpace(name) ? type.Name : name;
        return registry.RegisterForNavigation(type, registrationName);
    }

    public static IContainerRegistry RegisterForNavigation<TView, TViewModel>(this IContainerRegistry registry,
        string? name = null)
    {
        return registry.RegisterForNavigationWithViewModel<TViewModel>(typeof(TView), name);
    }

    private static IContainerRegistry RegisterForNavigationWithViewModel<TViewModel>(this IContainerRegistry registry,
        Type viewType, string? name)
    {
        var registrationName = string.IsNullOrWhiteSpace(name) ? viewType.Name : name;
        Prism.Mvvm.ViewModelLocationProvider.Register(viewType.ToString(), typeof(TViewModel));
        registry.Register(typeof(TViewModel));
        return registry.RegisterForNavigation(viewType, registrationName);
    }
}
