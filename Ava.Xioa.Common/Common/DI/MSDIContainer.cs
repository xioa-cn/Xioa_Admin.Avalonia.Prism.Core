using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Common.DI;

public class MSDIContainer : IContainerExtension
{
    private readonly IServiceProvider _serviceProvider;

    private readonly MSDIContainerRegistry _msdiContainerRegistry;
    private IScopedProvider? _currentScope;

    public MSDIContainer(IServiceProvider serviceProvider, MSDIContainerRegistry msdiContainerRegistry)
    {
        _serviceProvider = serviceProvider;
        _msdiContainerRegistry = msdiContainerRegistry;
    }

    public object Resolve(Type type)
    {
        return _serviceProvider.GetRequiredService(type);
    }

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
    {
        var tempCollection = new ServiceCollection();
        
        foreach (var param in parameters)
        {
            tempCollection.AddSingleton(param.Type, param.Instance);
        }
        
        tempCollection.AddSingleton<IServiceProvider>(_serviceProvider);
        
        var tempProvider = tempCollection.BuildServiceProvider();
        
        return ActivatorUtilities.CreateInstance(tempProvider, type);
    }

    public object Resolve(Type type, string name)
    {
        return _serviceProvider.GetKeyedServices(type, name).FirstOrDefault()??
               throw new InvalidOperationException($"Service of type {type.Name} with key {name} not found.");
    }

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
    {
        var tempCollection = new ServiceCollection();
        
        foreach (var param in parameters)
        {
            tempCollection.AddSingleton(param.Type, param.Instance);
        }
        
        tempCollection.AddSingleton<IServiceProvider>(_serviceProvider);
        
        var tempProvider = tempCollection.BuildServiceProvider();
        
        return tempProvider.GetKeyedService(type, name) ??
               throw new InvalidOperationException($"Service of type {type.Name} with key {name} not found.");
    }

    public IScopedProvider CreateScope()
    {
        var scope = _serviceProvider.CreateScope();
        var scopedProvider = new MSDIScopedProvider(scope, _msdiContainerRegistry, null);
        
        // 更新当前活动的作用域
        CurrentScope = scopedProvider;
        
        return scopedProvider;
    }

    // 返回当前活动的作用域，如果没有活动的作用域则返回 null
    public IScopedProvider? CurrentScope 
    { 
        get => _currentScope; 
        private set => _currentScope = value; 
    }

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        return _msdiContainerRegistry.RegisterInstance(type, instance);
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        return _msdiContainerRegistry.RegisterInstance(type, instance, name);
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        return _msdiContainerRegistry.RegisterSingleton(from, to);
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
    {
        return _msdiContainerRegistry.RegisterSingleton(from, to, name);
    }

    public IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod)
    {
        return _msdiContainerRegistry.RegisterSingleton(type, factoryMethod);
    }

    public IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        return _msdiContainerRegistry.RegisterSingleton(type, factoryMethod);
    }

    public IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes)
    {
        return _msdiContainerRegistry.RegisterManySingleton(type, serviceTypes);
    }

    public IContainerRegistry Register(Type from, Type to)
    {
        return _msdiContainerRegistry.Register(from, to);
    }

    public IContainerRegistry Register(Type from, Type to, string name)
    {
        return _msdiContainerRegistry.Register(from, to, name);
    }

    public IContainerRegistry Register(Type type, Func<object> factoryMethod)
    {
        return _msdiContainerRegistry.Register(type, factoryMethod);
    }

    public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        return _msdiContainerRegistry.Register(type, factoryMethod);
    }

    public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes)
    {
        return _msdiContainerRegistry.RegisterMany(type, serviceTypes);
    }

    public IContainerRegistry RegisterScoped(Type from, Type to)
    {
        return _msdiContainerRegistry.RegisterScoped(from, to);
    }

    public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod)
    {
        return _msdiContainerRegistry.RegisterScoped(type, factoryMethod);
    }

    public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        return _msdiContainerRegistry.RegisterScoped(type, factoryMethod);
    }

    public bool IsRegistered(Type type)
    {
        return _msdiContainerRegistry.IsRegistered(type);
    }

    public bool IsRegistered(Type type, string name)
    {
        return _msdiContainerRegistry.IsRegistered(type, name);
    }

    public void FinalizeExtension()
    {
        throw new NotImplementedException();
    }
}