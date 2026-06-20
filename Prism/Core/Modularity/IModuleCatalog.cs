namespace Prism.Modularity;

public interface IModuleCatalog
{
    IEnumerable<ModuleInfo> Modules { get; }

    IModuleCatalog AddModule(ModuleInfo moduleInfo);

    IModuleCatalog AddModule(IModuleInfo moduleInfo);
}
