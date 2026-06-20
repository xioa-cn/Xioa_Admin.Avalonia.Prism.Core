namespace Prism.Navigation.Regions;

public interface IRegionBehaviorFactory
{
    void AddIfMissing(string behaviorKey, Type behaviorType);

    bool ContainsKey(string behaviorKey);

    IEnumerable<KeyValuePair<string, Type>> GetBehaviors();
}