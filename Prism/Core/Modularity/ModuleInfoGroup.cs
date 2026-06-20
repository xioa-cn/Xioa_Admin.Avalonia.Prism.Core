namespace Prism.Modularity;

public sealed class ModuleInfoGroup : List<IModuleInfo>, IModuleCatalogItem
{
    public InitializationMode InitializationMode { get; set; } = InitializationMode.WhenAvailable;

    public string? Ref { get; set; }
}