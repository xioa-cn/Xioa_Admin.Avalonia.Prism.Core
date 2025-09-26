using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Common.DI;

public class MSDIScopedProvider : IScopedProvider
{
    private readonly IServiceScope _serviceScope;
    private readonly MSDIContainerRegistry _containerRegistry;
    private readonly IScopedProvider? _parentScope;
    
    public IScopedProvider? CurrentScope { get; }

    public MSDIScopedProvider(IServiceScope serviceScope, MSDIContainerRegistry containerRegistry, IScopedProvider? parentScope = null)
    {
        _serviceScope = serviceScope;
        _containerRegistry = containerRegistry;
        _parentScope = parentScope;
        CurrentScope = this; 
    }

    public object Resolve(Type type)
    {
        return _serviceScope.ServiceProvider.GetRequiredService(type);
    }

    public object Resolve(Type type, string name)
    {
        return _serviceScope.ServiceProvider.GetKeyedServices(type, name).FirstOrDefault() ??
               throw new InvalidOperationException($"Service of type {type.Name} with key {name} not found.");
    }

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
    {
        var tempCollection = new ServiceCollection();
        
        foreach (var param in parameters)
        {
            tempCollection.AddSingleton(param.Type, param.Instance);
        }
        
        tempCollection.AddSingleton<IServiceProvider>(_serviceScope.ServiceProvider);
        
        var tempProvider = tempCollection.BuildServiceProvider();
        
        return ActivatorUtilities.CreateInstance(tempProvider, type);
    }

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
    {
        var tempCollection = new ServiceCollection();
        
        foreach (var param in parameters)
        {
            tempCollection.AddSingleton(param.Type, param.Instance);
        }
        
        tempCollection.AddSingleton<IServiceProvider>(_serviceScope.ServiceProvider);
        
        var tempProvider = tempCollection.BuildServiceProvider();
        
        return tempProvider.GetKeyedService(type, name) ??
               throw new InvalidOperationException($"Service of type {type.Name} with key {name} not found.");
    }

    public IScopedProvider CreateScope()
    {
        var childScope = _serviceScope.ServiceProvider.CreateScope();
        return new MSDIScopedProvider(childScope, _containerRegistry, this);
    }


    public void Dispose()
    {
        _serviceScope.Dispose();
    }

    public IScopedProvider CreateChildScope()
    {
        var childScope = _serviceScope.ServiceProvider.CreateScope();
        return new MSDIScopedProvider(childScope, _containerRegistry, this);
    }

    public bool IsAttached { get; set; }
}