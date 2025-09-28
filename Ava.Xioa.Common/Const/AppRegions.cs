namespace Ava.Xioa.Common.Const;

public class AppRegions
{
    public static AppRegions? _instance;

    public static AppRegions Instance
    {
        get { return _instance ??= new AppRegions(); }
    }

    public string Main { get; } = nameof(MainRegion);
    
    public string Home { get; } = nameof(HomeRegion);
    
    
    public const string HomeRegion = nameof(HomeRegion);
    public const string MainRegion = nameof(MainRegion);
}