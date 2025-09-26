using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace Ava.Xioa.Common.Common.DI;

public class MSDIContainerRegistry : IContainerRegistry
{
    private readonly ServiceCollection _serviceCollection;

    public ServiceCollection ServiceCollection => _serviceCollection;
    
    public MSDIContainerRegistry()
    {
        _serviceCollection = new  ServiceCollection();
    }

    public MSDIContainer BuildServiceProvider()
    {
        var serviceProvider = this._serviceCollection.BuildServiceProvider();

        return new MSDIContainer(serviceProvider, this);
    }

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        _serviceCollection.AddSingleton(type, instance);
        return this;
    }

    public IContainerRegistry RegisterInstance(Type type, object instance, string name)
    {
        _serviceCollection.AddKeyedSingleton(type, name, instance);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to)
    {
        _serviceCollection.AddSingleton(from, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
    {
        _serviceCollection.AddKeyedSingleton(from, name, to);
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod)
    {
        _serviceCollection.AddSingleton(type, serviceProvider => factoryMethod());
        return this;
    }

    public IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        _serviceCollection.AddSingleton(type, serviceProvider => 
        {
            var containerProvider = new MSDIContainer(serviceProvider, this);
            return factoryMethod(containerProvider);
        });
        return this;
    }

    public IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes)
    {
        foreach (var serviceType in serviceTypes)
        {
            _serviceCollection.AddSingleton(serviceType, provider => provider.GetRequiredService(type));
        }
        _serviceCollection.AddSingleton(type);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to)
    {
        _serviceCollection.AddTransient(from, to);
        return this;
    }

    public IContainerRegistry Register(Type from, Type to, string name)
    {
        _serviceCollection.AddKeyedTransient(from, name, to);
        return this;
    }

    public IContainerRegistry Register(Type type, Func<object> factoryMethod)
    {
        _serviceCollection.AddTransient(type, serviceProvider => factoryMethod());
        return this;
    }

    public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        _serviceCollection.AddTransient(type, serviceProvider => 
        {
            var containerProvider = new MSDIContainer(serviceProvider, this);
            return factoryMethod(containerProvider);
        });
        return this;
    }

    public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes)
    {
        foreach (var serviceType in serviceTypes)
        {
            _serviceCollection.AddTransient(serviceType, provider => provider.GetRequiredService(type));
        }
        _serviceCollection.AddTransient(type);
        return this;
    }

    public IContainerRegistry RegisterScoped(Type from, Type to)
    {
        _serviceCollection.AddScoped(from, to);
        return this;
    }

    public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod)
    {
        _serviceCollection.AddScoped(type, serviceProvider => factoryMethod());
        return this;
    }

    public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod)
    {
        _serviceCollection.AddScoped(type, serviceProvider => 
        {
            var containerProvider = new MSDIContainer(serviceProvider, this);
            return factoryMethod(containerProvider);
        });
        return this;
    }

    public bool IsRegistered(Type type)
    {
        return _serviceCollection.Any(x => x.ServiceType == type);
    }

    public bool IsRegistered(Type type, string name)
    {
        return _serviceCollection.Any(x => x.ServiceType == type && x.ServiceKey?.ToString() == name);
    }
}