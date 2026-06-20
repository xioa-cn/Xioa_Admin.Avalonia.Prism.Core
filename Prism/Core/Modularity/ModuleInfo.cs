namespace Prism.Modularity;

public sealed class ModuleInfo : IModuleInfo, IModuleCatalogItem
{
    public ModuleInfo()
    {
    }

    public ModuleInfo(Type moduleType, string moduleName, InitializationMode initializationMode)
        : this(moduleName, moduleType,
            Array.Empty<string>())
    {
        InitializationMode = initializationMode;
    }

    public ModuleInfo(string moduleName, Type moduleType, params string[] dependsOn)
    {
        ModuleName = moduleName;
        ModuleType = moduleType;
        foreach (var dependency in dependsOn)
        {
            DependsOn.Add(dependency);
        }
    }


    public string ModuleName { get; set; } = string.Empty;

    public Type ModuleType { get; set; }

    public InitializationMode InitializationMode { get; set; } = InitializationMode.WhenAvailable;

    public ModuleState State { get; set; } = ModuleState.NotStarted;

    public string? Ref { get; set; }

    public IList<string> DependsOn { get; } = new List<string>();
}