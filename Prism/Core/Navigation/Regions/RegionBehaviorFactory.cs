using System;
using System.Collections.Generic;

namespace Prism.Navigation.Regions;

public sealed class RegionBehaviorFactory : IRegionBehaviorFactory
{
    private readonly Dictionary<string, Type> _behaviors = new(StringComparer.Ordinal);
    private readonly object _syncRoot = new();

    public void AddIfMissing(string behaviorKey, Type behaviorType)
    {
        lock (_syncRoot)
        {
            if (!_behaviors.ContainsKey(behaviorKey))
            {
                _behaviors.Add(behaviorKey, behaviorType);
            }
        }
    }

    public bool ContainsKey(string behaviorKey)
    {
        lock (_syncRoot)
        {
            return _behaviors.ContainsKey(behaviorKey);
        }
    }

    public IEnumerable<KeyValuePair<string, Type>> GetBehaviors()
    {
        lock (_syncRoot)
        {
            return new List<KeyValuePair<string, Type>>(_behaviors);
        }
    }
}