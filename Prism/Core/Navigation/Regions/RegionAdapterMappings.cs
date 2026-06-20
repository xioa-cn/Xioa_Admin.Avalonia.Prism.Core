using System;
using System.Collections.Generic;
using Prism.Ioc;

namespace Prism.Navigation.Regions;

public sealed class RegionAdapterMappings
{
    private readonly Dictionary<Type, RegionAdapterRegistration> _mappings = new();
    private readonly Dictionary<Type, RegionAdapterRegistration?> _mappingCache = new();
    private readonly object _syncRoot = new();

    public void RegisterMapping(Type controlType, Type adapterType)
    {
        if (!typeof(IRegionAdapter).IsAssignableFrom(adapterType))
        {
            throw new ArgumentException($"Adapter type {adapterType.FullName} must implement IRegionAdapter.", nameof(adapterType));
        }

        lock (_syncRoot)
        {
            _mappings[controlType] = new RegionAdapterRegistration(adapterType, null);
            _mappingCache.Clear();
        }
    }

    public void RegisterMapping(Type controlType, IRegionAdapter adapter)
    {
        lock (_syncRoot)
        {
            _mappings[controlType] = new RegionAdapterRegistration(null, adapter);
            _mappingCache.Clear();
        }
    }

    public bool TryGetMapping(Type controlType, out IRegionAdapter adapter)
    {
        lock (_syncRoot)
        {
            if (TryGetRegistrationCore(controlType, out var registration) && registration.Adapter is not null)
            {
                adapter = registration.Adapter;
                return true;
            }
        }

        adapter = null!;
        return false;
    }

    internal bool TryGetMapping(Type controlType, IContainerProvider container, out IRegionAdapter adapter)
    {
        lock (_syncRoot)
        {
            if (TryGetRegistrationCore(controlType, out var registration))
            {
                adapter = registration.Adapter ?? (IRegionAdapter)container.Resolve(registration.AdapterType!);
                return true;
            }
        }

        adapter = null!;
        return false;
    }

    private bool TryGetRegistrationCore(Type controlType, out RegionAdapterRegistration registration)
    {
        if (_mappingCache.TryGetValue(controlType, out var cachedRegistration))
        {
            if (cachedRegistration is { } foundRegistration)
            {
                registration = foundRegistration;
                return true;
            }

            registration = default;
            return false;
        }

        for (var current = controlType; current is not null; current = current.BaseType)
        {
            if (_mappings.TryGetValue(current, out registration))
            {
                _mappingCache[controlType] = registration;
                return true;
            }
        }

        foreach (var interfaceType in controlType.GetInterfaces())
        {
            if (_mappings.TryGetValue(interfaceType, out registration))
            {
                _mappingCache[controlType] = registration;
                return true;
            }
        }

        _mappingCache[controlType] = null;
        registration = default;
        return false;
    }

    private readonly record struct RegionAdapterRegistration(Type? AdapterType, IRegionAdapter? Adapter);
}