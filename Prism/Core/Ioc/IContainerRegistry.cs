namespace Prism.Ioc;

public interface IContainerRegistry
{
    IContainerRegistry Register(Type from, Type to);

    IContainerRegistry Register(Type type);
    
    IContainerRegistry RegisterScoped(Type from, Type to);

    IContainerRegistry RegisterSingleton(Type from, Type to);

    IContainerRegistry RegisterSingleton(Type type);

    IContainerRegistry RegisterInstance(Type type, object instance);

    IContainerRegistry Register(Type from, Type to, string name);

    IContainerRegistry Register(Type type, string name);

    IContainerRegistry RegisterSingleton(Type from, Type to, string name);

    IContainerRegistry RegisterInstance(Type type, object instance, string name);

    IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory);

    IContainerRegistry RegisterFactory(Type type, Func<IContainerProvider, object> factory, string name);

    IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory);

    IContainerRegistry RegisterSingletonFactory(Type type, Func<IContainerProvider, object> factory, string name);
}